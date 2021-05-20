using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    World world;
    BlockType typeOfBlock;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }
    private void Update()
    {
        if (!world.inUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;
        if(Input.GetMouseButtonDown(0))
        {
            if (CheckForSlot() != null)
                HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if (clickedSlot == null)
            return;

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;
        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if(!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if(cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);

            }
            if(cursorSlot.itemSlot.stack.id == clickedSlot.itemSlot.stack.id)
            {
                int stackTotal = cursorSlot.itemSlot.stack.amount + clickedSlot.itemSlot.stack.amount;
                if (stackTotal < world.blocktypes[clickedSlot.itemSlot.stack.id].maxStackSize)
                {
                    cursorSlot.itemSlot.stack.amount = stackTotal;
                    clickedSlot.itemSlot.EmptySlot();
                }
                
            }
        }
        // Point raised - stacking items of same type.  Not covered in video, but would check same item id and add the amounts together forming a stack
        // with max stack size of 64 (minecraft's max stack size).  A challenge for myself to add.  Was pointed out that this is tedious to do - we'll see...
        // adding and subtracting?
        // simplest way seems to be to do the following
        // - check that the IDs are the same (done)
        // if they are, add the stack sizes together into a new int (done)
        // take from clicked slot (not done) which already empties the clicked slot and puts that into the cursor's slot.
        // we already have a check for if an item exists, and are of different or empty block types, and to take all if they both have something.


    }

    private UIItemSlot CheckForSlot()
    {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();

        }
        return null;

    }
}
