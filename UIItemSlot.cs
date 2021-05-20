using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIItemSlot : MonoBehaviour
{
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public TextMeshProUGUI slotAmount;



    World world;

    private void Awake()
    {
        world = GameObject.Find("World").GetComponent<World>();
        //world = gameObject.GetComponent<World>();  This seems to give null reference errors the way it's used - will need to figure out why.
        // am trying to avoid using .Find as it's bad practice - i.e. what if the name changes?  I would need to update the name here, and anywhere
        // else that references "World).  Time-Consuming, and can be avoided.
    }

    public bool HasItem
    {
        get { if (itemSlot == null) return false; else return itemSlot.HasItem; }
    }

    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }
    public void UnLink()
    {
        itemSlot.unLinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {
            slotIcon.sprite = world.blocktypes[itemSlot.stack.id].icon;
            slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else
            Clear();
    }
    public void Clear()
    {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }
    private void OnDestroy()
    {
        if(isLinked)
        {
            itemSlot.unLinkUISlot();
        }
    }
}



public class ItemSlot
{
    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative;

    public ItemSlot (UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);

    }

    public ItemSlot (UIItemSlot _uiItemSlot, ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }
    public void unLinkUISlot()
    {
        uiItemSlot = null;

    }

    public void EmptySlot()
    {
        stack = null;
        if(uiItemSlot != null)
        {
            uiItemSlot.UpdateSlot();
        }
    }

    public int Take (int amt)
    {
        if (amt > stack.amount)
        {
            int _amt = stack.amount;
            EmptySlot();
            return _amt;
        }
        else if(amt <= stack.amount)
        {
            stack.amount -= amt;
            uiItemSlot.UpdateSlot();
            return amt;
        }
        else
        {
            EmptySlot();
            return amt;
        }
        
    }

    public ItemStack TakeAll()
    {
        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertStack (ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get { if (stack != null) return true; else return false; }
    }
};

