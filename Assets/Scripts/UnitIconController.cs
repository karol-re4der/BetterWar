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
    public TextMeshProUGUI SizeText;
    public Color BarColorFull = Color.green;
    public Color BarColorHalf = Color.yellow;
    public Color BarColorEmpty = Color.red;
    public Color BackgroundColorRegular = Color.white;
    public Color BackgroundColorSelected = Color.gray;
    public Color BackgroundColorHighlighted = Color.gray;

    private bool _selected = false;


    private int prevSize = 0;

    // Start is called before the first frame update
    void Start()
    { 

    }

    // Update is called once per frame
    void Update()
    {
        if(UnitGroup != null)
        {
            if (prevSize != UnitGroup.CurrentSize)
            {
                //Change text
                SizeText.text = UnitGroup.CurrentSize + "/" + UnitGroup.InitialSize;
                prevSize = UnitGroup.CurrentSize;

                //Resize bar
                float ratio = (float)UnitGroup.CurrentSize / UnitGroup.InitialSize;
                Color col = Color.Lerp(BarColorFull, BarColorEmpty, 1-ratio);
                SizeBar.GetComponent<Image>().color = col;
                SetLeft(SizeBar.GetComponent<RectTransform>(), 5);
                SetRight(SizeBar.GetComponent<RectTransform>(), 85-(80*ratio));
            }
        }
    }

    private void SetLeft(RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    private void SetRight(RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    #region mouse events
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        Globals.GetUserControls.SelectUnitByIcon(this);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (selected)
        {
            GetComponent<Image>().color = BackgroundColorSelected;
        }
        else
        {
            GetComponent<Image>().color = BackgroundColorRegular;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().color = BackgroundColorHighlighted;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_selected)
        {
            GetComponent<Image>().color = BackgroundColorSelected;
        }
        else
        {
            GetComponent<Image>().color = BackgroundColorRegular;
        }
    }
    #endregion
}
