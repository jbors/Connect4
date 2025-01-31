﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ConnectFour
{
  public class GameController : MonoBehaviour
  {

    [Range (3, 8)]
    public int numRows = 4;
    [Range (3, 8)]
    public int numColumns = 4;

    public int monteCarloRuns = 1000;
    
    [Tooltip ("How many pieces have to be connected to win.")]
    public int numPiecesToWin = 4;

    [Tooltip ("Allow diagonally connected Pieces?")]
    public bool allowDiagonally = true;
		
    public float dropTime = 4f;

    // Gameobjects
    public GameObject pieceRed;
    // PieceBlue is actually yellow, just to keep it confusing :/
    public GameObject pieceBlue;
    public GameObject pieceField;

    public GameObject winningText;
    public string playerWonText = "You Won!";
    public string playerLoseText = "You Lose!";
    public string drawText = "Draw!";

    public GameObject btnPlayAgain;
    bool btnPlayAgainTouching = false;
    Color btnPlayAgainOrigColor;
    Color btnPlayAgainHoverColor = new Color (255, 143, 4);

    GameObject gameObjectField;

    // temporary gameobject, holds the piece at mouse position until the mouse has clicked
    GameObject gameObjectTurn;

    /// <summary>
    /// The Game field.
    /// 0 = Empty
    /// 1 = Blue
    /// 2 = Red
    /// </summary>
    Field field;

    bool isLoading = true;
    bool isDropping = false;
    bool mouseButtonPressed = false;

    bool gameOver = false;
    bool isCheckingForWinner = false;

    // Use this for initialization
    void Start ()
    {
      int max = Mathf.Max (numRows, numColumns);

      if (numPiecesToWin > max)
        numPiecesToWin = max;

      CreateField ();

      //IsPlayersTurn = System.Convert.ToBoolean(Random.Range (0, 1));

      btnPlayAgainOrigColor = btnPlayAgain.GetComponent<Renderer> ().material.color;
    }

    /// <summary>
    /// Creates the field.
    /// </summary>
    void CreateField ()
    {
      winningText.SetActive (false);
      btnPlayAgain.SetActive (false);

      isLoading = true;

      gameObjectField = GameObject.Find ("Field");
      if (gameObjectField != null) {
        DestroyImmediate (gameObjectField);
      }
      gameObjectField = new GameObject ("Field");

      // create an empty field and instantiate the cells
      field = new Field (numRows, numColumns, numPiecesToWin, allowDiagonally);

      for (int x = 0; x < numColumns; x++) {
        for (int y = 0; y < numRows; y++) {
          GameObject g = Instantiate (pieceField, new Vector3 (x, y * -1, -1), Quaternion.identity) as GameObject;
          g.transform.parent = gameObjectField.transform;
        }
      }

      isLoading = false;
      gameOver = false;

      // center camera
      Camera.main.transform.position = new Vector3 (
        (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f), Camera.main.transform.position.z);

      winningText.transform.position = new Vector3 (
        (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f) + 1, winningText.transform.position.z);

      btnPlayAgain.transform.position = new Vector3 (
        (numColumns - 1) / 2.0f, -((numRows - 1) / 2.0f) - 1, btnPlayAgain.transform.position.z);
    }

    /// <summary>
    /// Spawns a piece at mouse position above the first row
    /// </summary>
    /// <returns>The piece.</returns>
    GameObject SpawnPiece ()
    {
      Vector3 spawnPos = Camera.main.ScreenToWorldPoint (Input.mousePosition);

      int chosenColumn = 0;

      //AI move
      if (!field.IsPlayersTurn)
      {
        //Play randomly on first move
        if (field.PiecesNumber == 0)
        {
          chosenColumn = field.GetRandomMove();
        } 
        else
        {
          //Gets all possible moves in this position
          List<int> moves = field.GetPossibleDrops();

          //Remember to .Clone() the field before using it for evaluation to avoid changing the current board state

          //TODO: replace this random move with a good one
          chosenColumn = field.GetRandomMove(); 
        }
      }
      
      GameObject g = Instantiate (
                    field.IsPlayersTurn ? pieceBlue : pieceRed, // is players turn = spawn blue, else spawn red
                    new Vector3 (
                      Mathf.Clamp (chosenColumn, 0, numColumns - 1), 
                      gameObjectField.transform.position.y + 1, 0), // spawn it above the first row
                    Quaternion.identity) as GameObject;

      return g;
    }

    private int Simulate (Field simulatedField, System.Random r)
    {
      if (simulatedField.CheckForVictory ()) {
        //Is it the AI turn?
        return !simulatedField.IsPlayersTurn ? -1 : 1;
      }
      while (simulatedField.ContainsEmptyCell ()) {
        int column = simulatedField.GetRandomMove (r);
        simulatedField.DropInColumn (column);

        if (simulatedField.CheckForVictory ()) {
          //Is it the AI turn?
          return !simulatedField.IsPlayersTurn ? 1 : -1;
        }
        simulatedField.SwitchPlayer ();
      }
      
      //No more moves possible, draw
      return 0;
    }
    
    void UpdatePlayAgainButton ()
    {
      RaycastHit hit;
      //ray shooting out of the camera from where the mouse is
      Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			
      if (Physics.Raycast (ray, out hit) && hit.collider.name == btnPlayAgain.name) {
        btnPlayAgain.GetComponent<Renderer> ().material.color = btnPlayAgainHoverColor;
        //check if the left mouse has been pressed down this frame
        if (Input.GetMouseButtonDown (0) || Input.touchCount > 0 && btnPlayAgainTouching == false) {
          btnPlayAgainTouching = true;

          Application.LoadLevel (0);
        }
      } else {
        btnPlayAgain.GetComponent<Renderer> ().material.color = btnPlayAgainOrigColor;
      }
			
      if (Input.touchCount == 0) {
        btnPlayAgainTouching = false;
      }
    }

    // Update is called once per frame
    void Update ()
    {
      if (isLoading)
        return;

      if (isCheckingForWinner)
        return;

      if (gameOver) {
        winningText.SetActive (true);
        btnPlayAgain.SetActive (true);

        UpdatePlayAgainButton ();

        return;
      }

      if (field.IsPlayersTurn) {
        if (gameObjectTurn == null) {
          gameObjectTurn = SpawnPiece ();
        } else {
          // update the objects position
          Vector3 pos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
          gameObjectTurn.transform.position = new Vector3 (
            Mathf.Clamp (pos.x, 0, numColumns - 1), 
            gameObjectField.transform.position.y + 1, 0);

          // click the left mouse button to drop the piece into the selected column
          if (Input.GetMouseButtonDown (0) && !mouseButtonPressed && !isDropping) {
            mouseButtonPressed = true;

            StartCoroutine (dropPiece (gameObjectTurn));
          } else {
            mouseButtonPressed = false;
          }
        }
      } else {
        if (gameObjectTurn == null) {
          gameObjectTurn = SpawnPiece ();
        } else {
          if (!isDropping)
            StartCoroutine (dropPiece (gameObjectTurn));
        }
      }
    }

    /// <summary>
    /// This method searches for a empty cell and lets 
    /// the object fall down into this cell
    /// </summary>
    /// <param name="gObject">Game Object.</param>
    IEnumerator dropPiece (GameObject gObject)
    {
      isDropping = true;

      Vector3 startPosition = gObject.transform.position;
      Vector3 endPosition = new Vector3 ();

      // round to a grid cell
      int x = Mathf.RoundToInt (startPosition.x);
      startPosition = new Vector3 (x, startPosition.y, startPosition.z);

      int y = field.DropInColumn (x);

      if (y != -1) {
        endPosition = new Vector3 (x, y * -1, startPosition.z);

        // Instantiate a new Piece, disable the temporary
        GameObject g = Instantiate (gObject) as GameObject;
        gameObjectTurn.GetComponent<Renderer> ().enabled = false;

        float distance = Vector3.Distance (startPosition, endPosition);

        float t = 0;
        while (t < 1) {
          t += Time.deltaTime * dropTime * ((numRows - distance) + 1);

          g.transform.position = Vector3.Lerp (startPosition, endPosition, t);
          yield return null;
        }

        g.transform.parent = gameObjectField.transform;

        // remove the temporary gameobject
        DestroyImmediate (gameObjectTurn);

        // run coroutine to check if someone has won
        StartCoroutine (Won ());

        // wait until winning check is done
        while (isCheckingForWinner)
          yield return null;

        field.SwitchPlayer ();
      }

      isDropping = false;

      yield return 0;
    }

    /// <summary>
    /// Check for Winner
    /// </summary>
    IEnumerator Won ()
    {
      isCheckingForWinner = true;

      gameOver = field.CheckForWinner ();

      // if Game Over update the winning text to show who has won
      if (gameOver == true) {
        winningText.GetComponent<TextMesh> ().text = field.IsPlayersTurn ? playerWonText : playerLoseText;
      } else {
        // check if there are any empty cells left, if not set game over and update text to show a draw
        if (!field.ContainsEmptyCell ()) {
          gameOver = true;
          winningText.GetComponent<TextMesh> ().text = drawText;
        }
      }

      isCheckingForWinner = false;

      yield return 0;
    }
  }
}
