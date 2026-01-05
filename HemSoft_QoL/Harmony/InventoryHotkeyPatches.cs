using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace HemSoft.QoL.Patches
{
    /// <summary>
    /// Harmony patches for inventory hotkey functionality.
    /// Hooks into XUiController.Update and filters for backpack window instances.
    /// </summary>
    [HarmonyPatch(typeof(XUiController))]
    [HarmonyPatch("Update")]
    public class InventoryHotkeyPatches
    {
        /// <summary>
        /// Called every frame for any XUiController update.
        /// Filters to only process XUiC_BackpackWindow instances.
        /// </summary>
        public static void Postfix(XUiController __instance)
        {
            // Only process backpack windows
            if (__instance is not XUiC_BackpackWindow backpackWindow) return;
            
            // Only process when game is running and a container is open
            if (!XUi.IsGameRunning()) return;
            if (backpackWindow.xui?.lootContainer == null) return;

            var config = HemSoftQoL.Config;
            if (config == null) return;

            // Quick Stack: Deposit items matching existing stacks in container
            if (config.QuickStack.IsPressed())
            {
                InventoryActions.QuickStack(backpackWindow);
            }
            // Stash All: Deposit all non-locked items
            else if (config.StashAll.IsPressed())
            {
                InventoryActions.StashAll(backpackWindow);
            }
            // Restock: Pull items from container to fill inventory stacks
            else if (config.Restock.IsPressed())
            {
                InventoryActions.Restock(backpackWindow);
            }
            // Sort Container: Sort items in the open container
            else if (config.SortContainer.IsPressed())
            {
                InventoryActions.SortContainer(backpackWindow);
            }
            // Sort Inventory: Sort items in player backpack
            else if (config.SortInventory.IsPressed())
            {
                InventoryActions.SortInventory(backpackWindow);
            }
        }
    }

    /// <summary>
    /// Inventory action implementations using 7D2D V2.5 APIs.
    /// </summary>
    public static class InventoryActions
    {
        /// <summary>
        /// Deposits items from player inventory that match existing stacks in the container.
        /// </summary>
        public static void QuickStack(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var container = xui.lootContainer;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var containerItems = container.items;
            var transferCount = 0;

            // Get backpack slots
            var backpackSlots = playerInventory.backpack.GetSlots();

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                // Check if this item type exists in the container
                foreach (var containerStack in containerItems)
                {
                    if (containerStack.IsEmpty()) continue;
                    if (!containerStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

                    // Found a match - try to stack
                    var countBefore = playerStack.count;
                    var result = container.TryStackItem(0, playerStack);
                    
                    if (result.anyMoved)
                    {
                        transferCount += countBefore - playerStack.count;

                        // Update player backpack slot
                        if (playerStack.count == 0)
                        {
                            playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                        }
                        else
                        {
                            playerInventory.backpack.SetSlot(i, playerStack, true);
                        }
                    }
                    break;
                }
            }

            if (transferCount > 0)
            {
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Quick Stack: Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Deposits all items from player inventory into the container.
        /// </summary>
        public static void StashAll(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var container = xui.lootContainer;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var transferCount = 0;
            var backpackSlots = playerInventory.backpack.GetSlots();

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var countBefore = playerStack.count;

                // Try to add to container (TryStackItem or AddItem)
                var result = container.TryStackItem(0, playerStack);
                if (result.anyMoved)
                {
                    transferCount += countBefore - playerStack.count;
                    if (playerStack.count == 0)
                    {
                        playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                    }
                    else
                    {
                        playerInventory.backpack.SetSlot(i, playerStack, true);
                    }
                }
                else if (container.AddItem(playerStack))
                {
                    transferCount += countBefore;
                    playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                }
            }

            if (transferCount > 0)
            {
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Stash All: Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Pulls items from container to fill existing stacks in player inventory.
        /// </summary>
        public static void Restock(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var container = xui.lootContainer;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var containerItems = container.items;
            var transferCount = 0;

            // Check player backpack for partial stacks
            var backpackSlots = playerInventory.backpack.GetSlots();

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var itemClass = playerStack.itemValue.ItemClass;
                if (itemClass == null) continue;

                var maxStack = itemClass.Stacknumber.Value;
                if (playerStack.count >= maxStack) continue; // Already full

                // Look for matching items in container
                for (var j = 0; j < containerItems.Length; j++)
                {
                    var containerStack = containerItems[j];
                    if (containerStack.IsEmpty()) continue;
                    if (!containerStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

                    // Calculate how many we can take
                    var spaceAvailable = maxStack - playerStack.count;
                    var toTransfer = Mathf.Min(spaceAvailable, containerStack.count);

                    if (toTransfer > 0)
                    {
                        playerStack.count += toTransfer;
                        containerStack.count -= toTransfer;
                        transferCount += toTransfer;

                        playerInventory.backpack.SetSlot(i, playerStack, true);

                        if (containerStack.count <= 0)
                        {
                            container.UpdateSlot(j, ItemStack.Empty.Clone());
                        }
                        else
                        {
                            container.UpdateSlot(j, containerStack);
                        }

                        // Check if player stack is now full
                        if (playerStack.count >= maxStack) break;
                    }
                }
            }

            if (transferCount > 0)
            {
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Restock: Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Sorts items in the container alphabetically by item name.
        /// </summary>
        public static void SortContainer(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var container = xui.lootContainer;

            if (container == null) return;

            var containerItems = container.items;
            var itemList = new List<ItemStack>();

            // Collect all non-empty items
            for (var i = 0; i < containerItems.Length; i++)
            {
                if (!containerItems[i].IsEmpty())
                {
                    itemList.Add(containerItems[i].Clone());
                }
            }

            // Sort alphabetically by localized name
            itemList.Sort((a, b) => 
            {
                var nameA = GetItemName(a);
                var nameB = GetItemName(b);
                return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
            });

            // Clear container and refill sorted
            for (var i = 0; i < containerItems.Length; i++)
            {
                if (i < itemList.Count)
                {
                    container.UpdateSlot(i, itemList[i]);
                }
                else
                {
                    container.UpdateSlot(i, ItemStack.Empty.Clone());
                }
            }

            PlaySound(xui, "ui_loot");
            HemSoftQoL.Log($"Sort Container: Sorted {itemList.Count} items");
        }

        /// <summary>
        /// Sorts items in the player's backpack alphabetically by item name.
        /// </summary>
        public static void SortInventory(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;

            if (playerInventory == null) return;

            var backpackSlots = playerInventory.backpack.GetSlots();
            var itemList = new List<ItemStack>();

            // Collect all non-empty items
            for (var i = 0; i < backpackSlots.Length; i++)
            {
                if (!backpackSlots[i].IsEmpty())
                {
                    itemList.Add(backpackSlots[i].Clone());
                }
            }

            // Sort alphabetically by localized name
            itemList.Sort((a, b) =>
            {
                var nameA = GetItemName(a);
                var nameB = GetItemName(b);
                return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
            });

            // Clear backpack and refill sorted
            for (var i = 0; i < backpackSlots.Length; i++)
            {
                if (i < itemList.Count)
                {
                    playerInventory.backpack.SetSlot(i, itemList[i], true);
                }
                else
                {
                    playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                }
            }

            PlaySound(xui, "ui_loot");
            HemSoftQoL.Log($"Sort Inventory: Sorted {itemList.Count} items");
        }

        private static string GetItemName(ItemStack stack)
        {
            if (stack.IsEmpty()) return string.Empty;
            var itemClass = stack.itemValue.ItemClass;
            return itemClass?.GetLocalizedItemName() ?? itemClass?.Name ?? string.Empty;
        }

        private static void PlaySound(XUi xui, string soundName)
        {
            xui?.playerUI?.entityPlayer?.PlayOneShot(soundName);
        }
    }
}
