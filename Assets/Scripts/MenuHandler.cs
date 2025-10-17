using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHandler : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter MenuMusicEvent {get; private set;}
    public GameObject PlayObject;
    public GameObject catScreen;
    public GameObject dogScreen;
    public GameObject lastScreen;
    
    void Start()
    {
        MenuMusicEvent = GetComponent<FMODUnity.StudioEventEmitter>();
    }

    public void CatNext()
    {
        catScreen.SetActive(false);
        dogScreen.SetActive(true);
    }
    
    public void DogNext()
    {
        dogScreen.SetActive(false);
        lastScreen.SetActive(true);
    }
    
    public void GameStart()
    {
        PlayObject.SetActive(true);
        Camera.main.GetComponent<PlayerVisibility>().enabled = true;
        Destroy(gameObject);
    }
}
