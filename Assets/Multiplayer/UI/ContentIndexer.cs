using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ContentPage
{
    Intro,
    Scene,
    Config,
    Sound,
    Vrc
}

[Serializable]
public class MenuPage
{
    public ContentPage page;
    public GameObject body;
    public GameObject[] subPages;
    private int _subPageIndex;

    public void Open(Button[] subpageButtons)
    {
        for(var i = 0; i < subpageButtons.Length; i++)
            subpageButtons[i].interactable = i < subPages.Length;
        
        body.SetActive(true);
        SetSubpage(0);
    }

    public void Close()
    {
        SetSubpage(0);
        body.SetActive(false);
    }

    public void SetSubpage(int index)
    {
        if (subPages.Length == 0) return;
        subPages[_subPageIndex].SetActive(false);
        _subPageIndex = index;
        subPages[_subPageIndex].SetActive(true);
    }
}

public class ContentIndexer : MonoBehaviour
{
    // todo, convert this to a list for extensibility
    [SerializeField] private MenuPage intro;
    [SerializeField] private MenuPage scene;
    [SerializeField] private MenuPage config;
    [SerializeField] private MenuPage sound;
    [SerializeField] private MenuPage vrc;
    [SerializeField] private Button[] subpageButtons;
    private readonly Dictionary<ContentPage, MenuPage> _pages = new();
    private ContentPage _currentPage;
    

    public void SetSceneBody() => SetPage(ContentPage.Scene);
    public void SetConfigBody() => SetPage(ContentPage.Config);
    public void SetSoundBody() => SetPage(ContentPage.Sound);
    public void SetIntroBody() => SetPage(ContentPage.Intro);
    public void SetVrcBody() => SetPage(ContentPage.Vrc);
    
    public void SetSubpage(int index) => _pages[_currentPage].SetSubpage(index);
    private void SetPage(ContentPage page)
    {
        _pages[_currentPage].Close();
        _currentPage = page;
        _pages[_currentPage].Open(subpageButtons);
    }

    private void Awake()
    {
        // todo, iterate list for extensibility
        _pages.Add(ContentPage.Intro, intro);
        _pages.Add(ContentPage.Scene, scene);
        _pages.Add(ContentPage.Config, config);
        _pages.Add(ContentPage.Sound, sound);
        _pages.Add(ContentPage.Vrc, vrc);

        foreach (var page in _pages) page.Value.Close();
        SetIntroBody();
    }

    private void OnDisable()
    {
        SetIntroBody();
    }
}