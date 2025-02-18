using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class SolitaireManager : MonoBehaviour
{
    [Header("UI Paneller")]
    public GameObject startPanel;  
    public GameObject winPanel;   

    private Solitaire solitaire;

    void Start()
    {

        if (startPanel != null)
            startPanel.SetActive(true);
        if (winPanel != null)
            winPanel.SetActive(false);

        solitaire = FindObjectOfType<Solitaire>();
    }

    void Update()
    {

        if (winPanel != null && !winPanel.activeSelf && solitaire != null)
        {
            bool win = true;

            foreach (GameObject top in solitaire.topPos)
            {
                Selectable sel = top.GetComponent<Selectable>();
                if (sel.value != 13)
                {
                    win = false;
                    break;
                }
            }
            if (win)
            {

                winPanel.SetActive(true);
            }
        }
    }


    public void StartGame()
    {
        if (startPanel != null)
            startPanel.SetActive(false);


        if (solitaire != null)
            solitaire.PlayCards();
    }


    public void RestartGame()
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    public void ReturnToMenu()
    {

        SceneManager.LoadScene("Menu");
    }
}