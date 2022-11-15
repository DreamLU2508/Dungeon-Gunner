using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Collections.Generic;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private GUIStyle roomNodeSelectedStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;
    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    // node layout values
    private const int nodePadding = 25;
    private const int nodeBorder = 12;
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;

    // width line
    private const float connectingLineWidth = 3f;
    private const float connectingLineArrowSize = 6f;

    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]

    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    private void OnEnable()
    {
        // Subscribe to the inspector selection changed event
        Selection.selectionChanged += InspectorSelectionChanged;

        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.white;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        // load room node types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    private void OnDisable()
    {
        // Unsubscribe to the inspector selection changed event
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    // Open the room node graph editor window if a room node graph scriptable object asset is double clicked in the inspector
    [OnOpenAsset(0)] // need namespace UnityEditor.Callbacks
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();

            currentRoomNodeGraph = roomNodeGraph;

            return true;
        }
        return false;
    }
    private void OnGUI()
    {
        if (currentRoomNodeGraph != null)
        {
            // Draw line if being dragged
            DrawDraggedLine();

            // Process Events
            ProcessEvents(Event.current);

            // Draw Connections Between Room Nodes
            DrawRoomConnections();

            // Draw Room Nodes
            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }

    private void ProcessEvents(Event currentEvent)
    {
        // Get room node that mouse is over if  it's null or not current being dragged
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        // if mouse isn't over a room node or we are currently dragging a line from the room node the process events
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        // else process room node events
        else
        {
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }

    // Check to see to mouse is over a room node - if so then return the room node else return null
    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for(int i = currentRoomNodeGraph.roomNodeList.Count -1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }
        return null;
    }

    // Process Room Node Graph Events
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            default:
                break;
        }
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if(currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);
            if(roomNode != null)
            {
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNoneToRoomNode(roomNode.id))
                {
                    roomNode.AddParentRoomNoneToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }

    // Process mouse drag events on the room node graph 
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        // Process right drag mouse event - draw line
        if (currentEvent.button == 1)
        {
            ProcessRightMouseDownEvent(currentEvent);
        }
    }

    // Process right drag mouse events on the room node graph 
    private void ProcessRightMouseDownEvent(Event currentEvent)
    {
        if(currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
        }
    }

    // Process mouse down events on the room node graph 
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // Process right click mouse down on graph event (show context menu)
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }

        // Process left click mouse down on graph event 
        if (currentEvent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }

    // Show context menu
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create room node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);

        menu.ShowAsContext();
    }

    // Create a room node at mouse position
    private void CreateRoomNode(object mousePositionObject)
    {
        if(currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f, 200f), roomNodeTypeList.list.Find(x => x.isEntrance));
        }

        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }

    // Create a room node at mouse position - overloaded to also pass in RoomNodeType
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        // Create room node scriptable object asset 
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        // Add room node to current room node graph
        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        // Set room node values 
        roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        // Add room node to room node graph scriptable object asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        AssetDatabase.SaveAssets();

        currentRoomNodeGraph.OnValidate();
    }

    // drag connecting line from room node
    public void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }

    public void DrawDraggedLine()
    {
        if(currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            Vector2 startPosition = currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center;
            Vector2 endPosition = currentRoomNodeGraph.linePosition;
            Vector2 startTangent = currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center;
            Vector2 endTangent = currentRoomNodeGraph.linePosition;

            // draw line
            Handles.DrawBezier(startPosition, endPosition, startTangent, endTangent, Color.white, null, connectingLineWidth);
        }
    }

    public void DrawRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.isSelected)
            {
                roomNode.Draw(roomNodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(roomNodeStyle);
            }
        }

        GUI.changed = true;
    }

    // Draw connections in the graph window room nodes
    public void DrawRoomConnections()
    {
        // loop through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.childRoomNodeIDList.Count > 0)
            {
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID))
                    {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);
                        
                        GUI.changed = true;
                    }
                }
            }
        }
    }

    // Draw connection line between the parent room node and child room node
    private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
    {
        Vector2 startPosition = parentRoomNode.rect.center;
        Vector2 endPosition =  childRoomNode.rect.center;

        Vector2 middlePosition = (startPosition + endPosition) / 2f;
        Vector2 direction = endPosition - startPosition;

        // draw ponit arrow
        Vector2 arrowTailPonit1 = middlePosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPonit2 = middlePosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowHeadPoint = middlePosition + direction.normalized * connectingLineArrowSize;

        // Draw arrow 
        Handles.DrawBezier(arrowHeadPoint, arrowTailPonit1, arrowHeadPoint, arrowTailPonit1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPonit2, arrowHeadPoint, arrowTailPonit2, Color.white, null, connectingLineWidth);

        // Draw connection line betwin the parent room node and child room node
        Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);
    }

    // Delete Selected Room Node Links
    private void DeleteSelectedRoomNodeLinks()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0)
            {
                for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0; i--)
                {
                    // Get child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNodeByID(roomNode.childRoomNodeIDList[i]);

                    if (childRoomNode != null && childRoomNode.isSelected)
                    {
                        // Remove childID from parent node 
                        roomNode.RemoveChildRoomNoneFromRoomNode(childRoomNode.id);

                        // Remove parentID from child node 
                        childRoomNode.RemoveParentRoomNoneFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        ClearAllSelectedRoomNodes();
    }

    // Delete Selected Room Nodes
    private void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();

        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && !roomNode.roomNodeType.isEntrance)
            {
                roomNodeDeletionQueue.Enqueue(roomNode);

                foreach (string childID in roomNode.childRoomNodeIDList)
                {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNodeByID(childID);

                    if (childRoomNode != null)
                    {
                        childRoomNode.RemoveParentRoomNoneFromRoomNode(roomNode.id);
                    }
                }

                foreach (string parentID in roomNode.parentRoomNodeIDList)
                {
                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNodeByID(parentID);

                    if (parentRoomNode != null)
                    {
                        parentRoomNode.RemoveChildRoomNoneFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        while (roomNodeDeletionQueue.Count > 0)
        {
            // Get room node from Queue
            RoomNodeSO roomNode = roomNodeDeletionQueue.Dequeue();

            // Remove room node from roomNodeDictionary
            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNode.id);

            // Remove room node from roomNodeList
            currentRoomNodeGraph.roomNodeList.Remove(roomNode);

            // Remove room node from Asset Database
            DestroyImmediate(roomNode, true);

            // Save Asset Database
            AssetDatabase.SaveAssets();
        }
    }

    // clear all room node selected
    private void ClearAllSelectedRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.isSelected = false;
                GUI.changed = true;
            }
        }
    }

    // Select all room nodes
    private void SelectAllRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (!roomNode.isSelected)
            {
                roomNode.isSelected = true;
                GUI.changed = true;
            }
        }
    }

    public void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }

    // Selection changed in the inspector 
    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }
}
