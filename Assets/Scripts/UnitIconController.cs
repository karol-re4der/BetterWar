using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UnitIconController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public UnitGroupController UnitGroup;
    public GameObject SizeBar;
    public GameObject ReloadBar;
    public GameObject SalvoTreshold;
    public TextMeshProUGUI SizeText;
    public Color BarColorFull = Color.green;
    public Color BarColorHalf = Color.yellow;
    public Color BarColorEmpty = Color.red;
    public Color BackgroundColorRegular = Color.white;
    public Color BackgroundColorSelected = Color.gray;
    public Color BackgroundColorHighlighted = Color.gray;

    private int _prevReloadProgress = 0;
    private int _prevSize = 0;

    // Start is called before the first frame update
    void Start()
    { 

    }

    // Update is called once per frame
    void Update()
    {
        if (UnitGroup != null)
        {
            if (_prevSize != UnitGroup.CurrentSize)
            {
                _prevSize = UnitGroup.CurrentSize;

                SizeText.text = UnitGroup.CurrentSize + "/" + UnitGroup.InitialSize;
                float ratio = UnitGroup.CurrentSize > 0 ? ((float)UnitGroup.CurrentSize / UnitGroup.InitialSize) : 0;
                Color col = Color.Lerp(BarColorFull, BarColorEmpty, 1 - ratio);
                //SizeBar.GetComponent<Scrollbar>().color = col;
                SizeBar.GetComponent<Scrollbar>().size = ratio;
            }

            //If in salvo mode, set indicator

            //Resize bar
            if (_prevReloadProgress != UnitGroup.CurrentReloadProgress)
            {
                float ratio = UnitGroup.CurrentInShootingPosition > 0 ? ((float)UnitGroup.CurrentReloadProgress / UnitGroup.CurrentInShootingPosition) : 0;
                Color col = Color.Lerp(BarColorFull, BarColorEmpty, 1 - ratio);

                ColorBlock block = ReloadBar.GetComponent<Scrollbar>().colors;
                block.normalColor = col;
                ReloadBar.GetComponent<Scrollbar>().colors = block;
                ReloadBar.GetComponent<Scrollbar>().size = ratio;

                UnitAction fireModeAction = UnitGroup.UnitActions.Find(x => x.ActionName.Equals("FireMode"));
                if (fireModeAction!=null && fireModeAction.GetCurrentState().Equals("Salvo"))
                {
                    SalvoTreshold.SetActive(true);
                    SalvoTreshold.GetComponent<Scrollbar>().value = UnitGroup.SalvoShootersRequired;
                }
                else
                {
                    SalvoTreshold.SetActive(false);
                }
            }
        }
    }

    #region mouse events
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        Globals.GetUserControls.SelectUnitByIcon(this);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        }
        else
        {
            transform.localScale = new Vector3(1f,1f,1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().color = BackgroundColorHighlighted;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().color = BackgroundColorRegular;
    }
    #endregion
}
