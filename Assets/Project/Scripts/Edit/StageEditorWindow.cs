using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Data_Script;

public class StageEditorWindow : EditorWindow
{
    private StageData currentStageData;

    private Vector2 scrollPos;
    private int selectedTab = 0;

    [MenuItem("Editor/Stage Editor")]
    public static void ShowWindow()
    {
        GetWindow<StageEditorWindow>("Stage Editor");
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (currentStageData == null)
        {
            EditorGUILayout.HelpBox("StageData를 선택하세요.", MessageType.Info);
            if (GUILayout.Button("StageData 선택"))
            {
                currentStageData = Selection.activeObject as StageData;
            }
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0:
                DrawBoardBlocksEditor();
                break;
            case 1:
                DrawPlayingBlocksEditor();
                break;
            case 2:
                DrawWallsEditor();
                break;
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("저장 (SO 갱신)"))
        {
            EditorUtility.SetDirty(currentStageData);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawToolbar()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, new[] { "Board Blocks", "Playing Blocks", "Walls" });

        EditorGUILayout.Space();
        currentStageData = (StageData)EditorGUILayout.ObjectField("StageData", currentStageData, typeof(StageData), false);
    }

    private void DrawBoardBlocksEditor()
    {
        EditorGUILayout.LabelField("Board Blocks", EditorStyles.boldLabel);
        if (GUILayout.Button("BoardBlock 추가"))
        {
            currentStageData.boardBlocks.Add(new BoardBlockData());
        }

        for (int i = 0; i < currentStageData.boardBlocks.Count; i++)
        {
            var block = currentStageData.boardBlocks[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Block {i}");

            block.x = EditorGUILayout.IntField("X", block.x);
            block.y = EditorGUILayout.IntField("Y", block.y);

            EditorGUILayout.LabelField("Color Types:");
            for (int j = 0; j < block.colorType.Count; j++)
            {
                block.colorType[j] = (ColorType)EditorGUILayout.EnumPopup($"Color {j}", block.colorType[j]);
            }

            if (GUILayout.Button("ColorType 추가"))
            {
                block.colorType.Add(ColorType.None);
            }

            if (GUILayout.Button("삭제"))
            {
                currentStageData.boardBlocks.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawPlayingBlocksEditor()
{
    EditorGUILayout.LabelField("Playing Blocks", EditorStyles.boldLabel);

    if (GUILayout.Button("PlayingBlock 추가"))
    {
        currentStageData.playingBlocks.Add(new PlayingBlockData());
    }

    for (int i = 0; i < currentStageData.playingBlocks.Count; i++)
    {
        var playingBlock = currentStageData.playingBlocks[i];

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"PlayingBlock {i}");

        // Center Position
        Vector2 centerFloat = EditorGUILayout.Vector2Field("Center Pos", playingBlock.center);
        playingBlock.center = new Vector2Int(Mathf.RoundToInt(centerFloat.x), Mathf.RoundToInt(centerFloat.y));

        // ColorType 설정
        playingBlock.colorType = (ColorType)EditorGUILayout.EnumPopup("Color Type", playingBlock.colorType);

        // Gimmick 설정
        EditorGUILayout.LabelField("Gimmicks:");
        if (playingBlock.gimmicks == null)
            playingBlock.gimmicks = new List<GimmickData>();

        for (int j = 0; j < playingBlock.gimmicks.Count; j++)
        {
            var gimmick = playingBlock.gimmicks[j];
            gimmick.gimmickType = EditorGUILayout.TextField($"Gimmick {j}", gimmick.gimmickType);

            if (GUILayout.Button($"Gimmick {j} 삭제"))
            {
                playingBlock.gimmicks.RemoveAt(j);
                break;
            }
        }

        if (GUILayout.Button("Gimmick 추가"))
        {
            playingBlock.gimmicks.Add(new GimmickData { gimmickType = "None" });
        }

        // Shapes 설정
        EditorGUILayout.LabelField("Shapes (Offsets):");
        if (playingBlock.shapes == null)
            playingBlock.shapes = new List<ShapeData>();

        for (int j = 0; j < playingBlock.shapes.Count; j++)
        {
            var shape = playingBlock.shapes[j];
            Vector2 offsetFloat = EditorGUILayout.Vector2Field($"Offset {j}", playingBlock.shapes[j].offset);
            playingBlock.shapes[j].offset = new Vector2Int(Mathf.RoundToInt(offsetFloat.x), Mathf.RoundToInt(offsetFloat.y));


            if (GUILayout.Button($"Shape {j} 삭제"))
            {
                playingBlock.shapes.RemoveAt(j);
                break;
            }
        }

        if (GUILayout.Button("Shape 추가"))
        {
            playingBlock.shapes.Add(new ShapeData { offset = Vector2Int.zero });
        }

        if (GUILayout.Button("삭제"))
        {
            currentStageData.playingBlocks.RemoveAt(i);
            break;
        }

        EditorGUILayout.EndVertical();
    }
}


    private void DrawWallsEditor()
    {
        EditorGUILayout.LabelField("Walls", EditorStyles.boldLabel);
        if (GUILayout.Button("Wall 추가"))
        {
            currentStageData.Walls.Add(new WallData());
        }

        for (int i = 0; i < currentStageData.Walls.Count; i++)
        {
            var wall = currentStageData.Walls[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Wall {i}");

            wall.x = EditorGUILayout.IntField("X", wall.x);
            wall.y = EditorGUILayout.IntField("Y", wall.y);
            wall.WallDirection = (ObjectPropertiesEnum.WallDirection)EditorGUILayout.EnumPopup("Direction", wall.WallDirection);
            wall.length = EditorGUILayout.IntField("Length", wall.length);
            wall.wallColor = (ColorType)EditorGUILayout.EnumPopup("Color", wall.wallColor);
            wall.wallGimmickType = (WallGimmickType)EditorGUILayout.EnumPopup("Gimmick", wall.wallGimmickType);

            if (GUILayout.Button("삭제"))
            {
                currentStageData.Walls.RemoveAt(i);
                break;
            }
            

            EditorGUILayout.EndVertical();
        }
    }
}
