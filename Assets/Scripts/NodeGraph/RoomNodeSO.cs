using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Editor Code

    // The following code should only be run in the Unity Editor
#if UNITY_EDITOR

    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    // Initialise node
    public void Initialise(Rect rect, RoomNodeGraphSO roomNodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = roomNodeGraph;
        this.roomNodeType = roomNodeType;

        // load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }


    // Draw node nodestyle 
    public void Draw(GUIStyle nodestyle)
    {
        // Draw Node Box Using Begin Area
        GUILayout.BeginArea(rect, nodestyle);

        // Start Region To Detect Popup Selection Changes
        EditorGUI.BeginChangeCheck();

        if(parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            // Display a popup using the RoomNodeType name values that can selected from (default to the currently set roomNodeType)
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(this);
        }

        GUILayout.EndArea();
    }

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomTypeArray = new string[roomNodeTypeList.list.Count];

        for(int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomTypeArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }

        return roomTypeArray;
    }

    // process events room node
    public void ProcessEvents (Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            default:
                break;
        }
    }

    // Process Mouse Down Event
    public void ProcessMouseDownEvent(Event currentEvent)
    {
        if(currentEvent.button == 0) 
        {
            ProcessLeftClickDownEvent();
        }
        if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }

    // Process Right Click Down Event
    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    // Process Left Click Down Event
    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;

        //Toggle node selection
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    // Process Mouse up Event
    public void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    // Process Left Click up Event
    private void ProcessLeftClickUpEvent()
    {
        if(isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }

    // Process Mouse Drag Event
    public void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    // Process Left Mouse Drag Event
    public void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;
        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    // Drag Node
    public void DragNode (Vector2 delta)
    {
        rect.position = rect.position + delta;
        EditorUtility.SetDirty(this);
    }

    // Add parentID to node (return true if the node has been added, false otherwise)
    public bool AddParentRoomNoneToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    // Add childID to node (return true if the node has been added, false otherwise)
    public bool AddChildRoomNoneToRoomNode(string childID)
    {
        // check child node can be added validly to parent
        if (IsChildRoomValidate(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
    }

    private bool IsChildRoomValidate(string childID)
    {
        bool isConnectedBossNodeAlready = false;

        // check if there is there alreary a connected boss room in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
            {
                isConnectedBossNodeAlready = true;  
            }
        }

        // if the child node has a type of boss room and there is already a connected boss room node then return false
        if (roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
            return false;

        // if child node has a type of none then return fasle
        if (roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isNone)
            return false;

        // if the node already has a child with this childID then return false 
        if (childRoomNodeIDList.Contains(childID))
            return false;

        // if this node ID and the child ID are the same then return false
        if (id == childID) 
            return false;

        // if the child ID is already in the parentID list then return false
        if (parentRoomNodeIDList.Contains(childID))
            return false;

        // if the child node already has a parent then return false
        if (roomNodeGraph.GetRoomNodeByID(childID).parentRoomNodeIDList.Count > 0)
            return false;

        // if the child is a corridor and this node is a corridor then return false
        if (roomNodeType.isCorridor && roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isCorridor)
            return false;

        // if the child is a room node and this node is a room node then return false
        if (!roomNodeType.isCorridor && !roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isCorridor)
            return false;

        // if adding a corridor check that this node has < the maximum permitted child corridors
        if (roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;

        // if the child is an entrance then return false - the entrance must always be the top level parent node 
        if (roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isEntrance)
            return false;

        // if adding a room to corridor check that this corridor node doesn't already have a room added
        if (!roomNodeGraph.GetRoomNodeByID(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;

        return true;
    }

#endif
    #endregion Editor Code
}
