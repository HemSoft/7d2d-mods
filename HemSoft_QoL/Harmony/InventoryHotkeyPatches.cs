using HarmonyLib;
using UnityEngine;

namespace HemSoft.QoL.Patches
{
    /// <summary>
    /// Harmony patches for inventory hotkey functionality.
    /// Hooks into XUiC_BackpackWindow.Update to check for hotkey presses.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_BackpackWindow))]
    [HarmonyPatch("Update")]
    public class InventoryHotkeyPatches
    {
        /// <summary>
        /// Called every frame when backpack window is active.
        /// Checks for hotkey presses and triggers inventory actions.
        /// </summary>
        public static void Postfix(XUiC_BackpackWindow __instance)
        {
            // Only process when game is running and a container is open
            if (!XUi.IsGameRunning()) return;
            if (__instance.xui?.lootContainer == null) return;

            var config = HemSoftQoL.Config;
            if (config == null) return;

            // Quick Stack: Deposit items matching existing stacks in container
            if (config.QuickStack.IsPressed())
            {
                InventoryActions.QuickStack(__instance);
            }
            // Stash All: Deposit all non-locked items
            else if (config.StashAll.IsPressed())
            {
                InventoryActions.StashAll(__instance);
            }
            // Restock: Pull items from container to fill inventory stacks
            else if (config.Restock.IsPressed())
            {
                InventoryActions.Restock(__instance);
            }
            // Loot All: Take everything from container
            else if (config.LootAll.IsPressed())
            {
                InventoryActions.LootAll(__instance);
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
        /// Takes all items from the container into player inventory.
        /// </summary>
        public static void LootAll(XUiC_BackpackWindow backpackWindow)
        {
            var xui = backpackWindow.xui;
            var container = xui.lootContainer;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var containerItems = container.items;
            var transferCount = 0;

            for (var i = 0; i < containerItems.Length; i++)
            {
                var containerStack = containerItems[i];
                if (containerStack.IsEmpty()) continue;

                var countBefore = containerStack.count;

                if (playerInventory.AddItem(containerStack.Clone()))
                {
                    transferCount += countBefore;
                    container.UpdateSlot(i, ItemStack.Empty.Clone());
                }
            }

            if (transferCount > 0)
            {
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Loot All: Took {transferCount} items");
            }
        }

        private static void PlaySound(XUi xui, string soundName)
        {
            xui?.playerUI?.entityPlayer?.PlayOneShot(soundName);
        }
    }
}
