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
    /// Supports both regular containers (ITileEntityLootable) and vehicle storage (XUiM_Vehicle).
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
            
            // Only process when game is running
            if (!XUi.IsGameRunning()) return;

            var xui = backpackWindow.xui;
            var config = HemSoftQoL.Config;
            if (config == null) return;

            // Check which storage type is open
            var windowManager = xui.playerUI?.windowManager;
            var isLootingOpen = windowManager?.IsWindowOpen("looting") ?? false;
            var isVehicleStorageOpen = windowManager?.IsWindowOpen("vehiclestorage") ?? false;

            // If neither is open, no action needed
            if (!isLootingOpen && !isVehicleStorageOpen) return;

            // Route to appropriate handler based on storage type
            if (isVehicleStorageOpen)
            {
                // Vehicle storage is open - use vehicle-specific methods (XUiM_Vehicle API)
                HandleVehicleStorageHotkeys(backpackWindow, config);
            }
            else if (isLootingOpen)
            {
                // Regular container is open - use ITileEntityLootable methods
                HandleContainerHotkeys(backpackWindow, config);
            }
        }

        /// <summary>
        /// Handles hotkey actions for regular containers (chests, loot bags, etc.)
        /// </summary>
        private static void HandleContainerHotkeys(XUiC_BackpackWindow backpackWindow, HotkeyConfig config)
        {
            var container = backpackWindow.xui.lootContainer;
            if (container == null) return;

            if (config.QuickStack.IsPressed())
            {
                ContainerActions.QuickStack(backpackWindow, container);
            }
            else if (config.StashAll.IsPressed())
            {
                ContainerActions.StashAll(backpackWindow, container);
            }
            else if (config.Restock.IsPressed())
            {
                ContainerActions.Restock(backpackWindow, container);
            }
            else if (config.SortContainer.IsPressed())
            {
                ContainerActions.SortContainer(backpackWindow, container);
            }
            else if (config.SortInventory.IsPressed())
            {
                ContainerActions.SortInventory(backpackWindow);
            }
        }

        /// <summary>
        /// Handles hotkey actions for vehicle storage.
        /// Vehicle storage is accessed through XUiC_ItemStackGrid from the window controller,
        /// NOT through EntityVehicle.lootContainer (that's a different container).
        /// </summary>
        private static void HandleVehicleStorageHotkeys(XUiC_BackpackWindow backpackWindow, HotkeyConfig config)
        {
            var xui = backpackWindow.xui;
            var entityVehicle = xui.vehicle;
            
            if (entityVehicle == null) return;

            var vehicleObj = entityVehicle.GetVehicle();
            if (vehicleObj == null) return;

            if (config.QuickStack.IsPressed())
            {
                VehicleActions.QuickStack(backpackWindow, entityVehicle, vehicleObj);
            }
            else if (config.StashAll.IsPressed())
            {
                VehicleActions.StashAll(backpackWindow, entityVehicle, vehicleObj);
            }
            else if (config.Restock.IsPressed())
            {
                VehicleActions.Restock(backpackWindow, entityVehicle, vehicleObj);
            }
            else if (config.SortContainer.IsPressed())
            {
                VehicleActions.SortVehicleStorage(backpackWindow, entityVehicle, vehicleObj);
            }
            else if (config.SortInventory.IsPressed())
            {
                ContainerActions.SortInventory(backpackWindow);
            }
        }
    }

    /// <summary>
    /// Vehicle storage inventory actions.
    /// IMPORTANT: Vehicle storage is accessed through XUiC_ItemStackGrid from the "vehiclestorage" window,
    /// NOT through EntityVehicle.lootContainer (that's a different container for vehicle destruction/corpse loot).
    /// </summary>
    public static class VehicleActions
    {
        /// <summary>
        /// Gets the ItemStackGrid from the vehicle storage window - this contains the actual storage items.
        /// </summary>
        private static XUiC_ItemStackGrid GetVehicleStorageGrid(XUi xui)
        {
            try
            {
                var window = xui.playerUI.windowManager.GetWindow("vehiclestorage");
                if (window == null) return null;
                
                var controller = ((XUiWindowGroup)window).Controller;
                if (controller == null) return null;
                
                return controller.GetChildByType<XUiC_ItemStackGrid>();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Updates the vehicle storage grid UI after modifying slots.
        /// </summary>
        private static void UpdateVehicleStorageUI(XUiC_ItemStackGrid grid, ItemStack[] slots)
        {
            // Get the individual stack controllers and update each one
            var stackControllers = grid.GetItemStackControllers();
            if (stackControllers != null)
            {
                for (int i = 0; i < stackControllers.Length && i < slots.Length; i++)
                {
                    if (stackControllers[i] != null)
                    {
                        stackControllers[i].ItemStack = slots[i];
                        stackControllers[i].ForceRefreshItemStack();
                    }
                }
            }
            grid.IsDirty = true;
        }

        /// <summary>
        /// Deposits items from player inventory that match existing items in vehicle storage.
        /// </summary>
        public static void QuickStack(XUiC_BackpackWindow backpackWindow, EntityVehicle entityVehicle, Vehicle vehicle)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;
            if (playerInventory == null) return;

            var grid = GetVehicleStorageGrid(xui);
            if (grid == null) return;

            var vehicleSlots = grid.GetSlots();
            if (vehicleSlots == null) return;

            var backpackSlots = playerInventory.backpack.GetSlots();
            var transferCount = 0;

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                // Check if this item type exists in vehicle storage
                var foundMatch = false;
                foreach (var vehicleStack in vehicleSlots)
                {
                    if (vehicleStack.IsEmpty()) continue;
                    if (!vehicleStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;
                    foundMatch = true;
                    break;
                }

                if (!foundMatch) continue;

                // Try to stack into existing slots first
                var countBefore = playerStack.count;
                for (var j = 0; j < vehicleSlots.Length && playerStack.count > 0; j++)
                {
                    var vehicleStack = vehicleSlots[j];
                    if (vehicleStack.IsEmpty()) continue;
                    if (!vehicleStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

                    var itemClass = vehicleStack.itemValue.ItemClass;
                    if (itemClass == null) continue;

                    var maxStack = itemClass.Stacknumber.Value;
                    var spaceAvailable = maxStack - vehicleStack.count;
                    if (spaceAvailable <= 0) continue;

                    var toTransfer = Mathf.Min(spaceAvailable, playerStack.count);
                    vehicleStack.count += toTransfer;
                    playerStack.count -= toTransfer;
                    transferCount += toTransfer;
                }

                // Update backpack slot
                if (playerStack.count != countBefore)
                {
                    if (playerStack.count <= 0)
                    {
                        playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                    }
                    else
                    {
                        playerInventory.backpack.SetSlot(i, playerStack, true);
                    }
                }
            }

            if (transferCount > 0)
            {
                // Update the grid UI
                UpdateVehicleStorageUI(grid, vehicleSlots);
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Quick Stack (Vehicle): Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Deposits all items from player inventory into vehicle storage.
        /// </summary>
        public static void StashAll(XUiC_BackpackWindow backpackWindow, EntityVehicle entityVehicle, Vehicle vehicle)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;
            if (playerInventory == null) return;

            var grid = GetVehicleStorageGrid(xui);
            if (grid == null) return;

            var vehicleSlots = grid.GetSlots();
            if (vehicleSlots == null) return;

            var backpackSlots = playerInventory.backpack.GetSlots();
            var transferCount = 0;

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var countBefore = playerStack.count;
                
                // Try to stack into existing matching slots first
                for (var j = 0; j < vehicleSlots.Length && playerStack.count > 0; j++)
                {
                    var vehicleStack = vehicleSlots[j];
                    if (vehicleStack.IsEmpty()) continue;
                    if (!vehicleStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

                    var itemClass = vehicleStack.itemValue.ItemClass;
                    if (itemClass == null) continue;

                    var maxStack = itemClass.Stacknumber.Value;
                    var spaceAvailable = maxStack - vehicleStack.count;
                    if (spaceAvailable <= 0) continue;

                    var toTransfer = Mathf.Min(spaceAvailable, playerStack.count);
                    vehicleStack.count += toTransfer;
                    playerStack.count -= toTransfer;
                    transferCount += toTransfer;
                }

                // If still have items, try to add to empty slots
                if (playerStack.count > 0)
                {
                    for (var j = 0; j < vehicleSlots.Length && playerStack.count > 0; j++)
                    {
                        if (!vehicleSlots[j].IsEmpty()) continue;

                        // Put remaining stack in empty slot
                        vehicleSlots[j] = playerStack.Clone();
                        transferCount += playerStack.count;
                        playerStack.count = 0;
                    }
                }

                // Update backpack slot
                if (playerStack.count != countBefore)
                {
                    if (playerStack.count <= 0)
                    {
                        playerInventory.backpack.SetSlot(i, ItemStack.Empty.Clone(), true);
                    }
                    else
                    {
                        playerInventory.backpack.SetSlot(i, playerStack, true);
                    }
                }
            }

            if (transferCount > 0)
            {
                UpdateVehicleStorageUI(grid, vehicleSlots);
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Stash All (Vehicle): Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Pulls items from vehicle storage to fill existing stacks in player inventory.
        /// </summary>
        public static void Restock(XUiC_BackpackWindow backpackWindow, EntityVehicle entityVehicle, Vehicle vehicle)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;
            if (playerInventory == null) return;

            var grid = GetVehicleStorageGrid(xui);
            if (grid == null) return;

            var vehicleSlots = grid.GetSlots();
            if (vehicleSlots == null) return;

            var backpackSlots = playerInventory.backpack.GetSlots();
            var transferCount = 0;

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var itemClass = playerStack.itemValue.ItemClass;
                if (itemClass == null) continue;

                var maxStack = itemClass.Stacknumber.Value;
                if (playerStack.count >= maxStack) continue; // Already full

                // Look for matching items in vehicle storage
                for (var j = 0; j < vehicleSlots.Length; j++)
                {
                    var vehicleStack = vehicleSlots[j];
                    if (vehicleStack.IsEmpty()) continue;
                    if (!vehicleStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

                    // Calculate how many we can take
                    var spaceAvailable = maxStack - playerStack.count;
                    var toTransfer = Mathf.Min(spaceAvailable, vehicleStack.count);

                    if (toTransfer > 0)
                    {
                        playerStack.count += toTransfer;
                        vehicleStack.count -= toTransfer;
                        transferCount += toTransfer;

                        playerInventory.backpack.SetSlot(i, playerStack, true);

                        if (vehicleStack.count <= 0)
                        {
                            vehicleSlots[j] = ItemStack.Empty.Clone();
                        }

                        // Check if player stack is now full
                        if (playerStack.count >= maxStack) break;
                    }
                }
            }

            if (transferCount > 0)
            {
                UpdateVehicleStorageUI(grid, vehicleSlots);
                PlaySound(xui, "ui_loot");
                HemSoftQoL.Log($"Restock (Vehicle): Moved {transferCount} items");
            }
        }

        /// <summary>
        /// Sorts items in vehicle storage alphabetically by item name.
        /// </summary>
        public static void SortVehicleStorage(XUiC_BackpackWindow backpackWindow, EntityVehicle entityVehicle, Vehicle vehicle)
        {
            var xui = backpackWindow.xui;

            var grid = GetVehicleStorageGrid(xui);
            if (grid == null) return;

            var vehicleSlots = grid.GetSlots();
            if (vehicleSlots == null) return;

            var itemList = new List<ItemStack>();

            // Collect all non-empty items
            for (var i = 0; i < vehicleSlots.Length; i++)
            {
                if (!vehicleSlots[i].IsEmpty())
                {
                    itemList.Add(vehicleSlots[i].Clone());
                }
            }

            // Sort alphabetically by localized name, then by quality
            itemList.Sort((a, b) =>
            {
                var nameA = GetItemName(a);
                var nameB = GetItemName(b);
                
                // Primary sort: alphabetically by name
                var nameComparison = string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0) return nameComparison;
                
                // Secondary sort: by quality (ascending - lower quality first, matching game default)
                var qualityA = a.itemValue.Quality;
                var qualityB = b.itemValue.Quality;
                return qualityA.CompareTo(qualityB);
            });

            // Clear slots and refill sorted
            for (var i = 0; i < vehicleSlots.Length; i++)
            {
                if (i < itemList.Count)
                {
                    vehicleSlots[i] = itemList[i];
                }
                else
                {
                    vehicleSlots[i] = ItemStack.Empty.Clone();
                }
            }

            UpdateVehicleStorageUI(grid, vehicleSlots);
            PlaySound(xui, "ui_loot");
            HemSoftQoL.Log($"Sort Vehicle Storage: Sorted {itemList.Count} items");
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

    /// <summary>
    /// Regular container inventory actions using ITileEntityLootable APIs.
    /// </summary>
    public static class ContainerActions
    {
        /// <summary>
        /// Deposits items from player inventory that match existing stacks in the container.
        /// </summary>
        public static void QuickStack(XUiC_BackpackWindow backpackWindow, ITileEntityLootable container)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var containerItems = container.items;
            if (containerItems == null) return;
            
            var transferCount = 0;
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
        public static void StashAll(XUiC_BackpackWindow backpackWindow, ITileEntityLootable container)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var transferCount = 0;
            var backpackSlots = playerInventory.backpack.GetSlots();

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var countBefore = playerStack.count;

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
        public static void Restock(XUiC_BackpackWindow backpackWindow, ITileEntityLootable container)
        {
            var xui = backpackWindow.xui;
            var playerInventory = xui.PlayerInventory;

            if (container == null || playerInventory == null) return;

            var containerItems = container.items;
            if (containerItems == null) return;
            
            var transferCount = 0;
            var backpackSlots = playerInventory.backpack.GetSlots();

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                var playerStack = backpackSlots[i];
                if (playerStack.IsEmpty()) continue;

                var itemClass = playerStack.itemValue.ItemClass;
                if (itemClass == null) continue;

                var maxStack = itemClass.Stacknumber.Value;
                if (playerStack.count >= maxStack) continue;

                for (var j = 0; j < containerItems.Length; j++)
                {
                    var containerStack = containerItems[j];
                    if (containerStack.IsEmpty()) continue;
                    if (!containerStack.itemValue.type.Equals(playerStack.itemValue.type)) continue;

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
        public static void SortContainer(XUiC_BackpackWindow backpackWindow, ITileEntityLootable container)
        {
            var xui = backpackWindow.xui;

            if (container == null) return;

            var containerItems = container.items;
            if (containerItems == null) return;
            
            var itemList = new List<ItemStack>();

            for (var i = 0; i < containerItems.Length; i++)
            {
                if (!containerItems[i].IsEmpty())
                {
                    itemList.Add(containerItems[i].Clone());
                }
            }

            itemList.Sort((a, b) => 
            {
                var nameA = GetItemName(a);
                var nameB = GetItemName(b);
                
                // Primary sort: alphabetically by name
                var nameComparison = string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0) return nameComparison;
                
                // Secondary sort: by quality (ascending - lower quality first, matching game default)
                var qualityA = a.itemValue.Quality;
                var qualityB = b.itemValue.Quality;
                return qualityA.CompareTo(qualityB);
            });

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

            for (var i = 0; i < backpackSlots.Length; i++)
            {
                if (!backpackSlots[i].IsEmpty())
                {
                    itemList.Add(backpackSlots[i].Clone());
                }
            }

            itemList.Sort((a, b) =>
            {
                var nameA = GetItemName(a);
                var nameB = GetItemName(b);
                
                // Primary sort: alphabetically by name
                var nameComparison = string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0) return nameComparison;
                
                // Secondary sort: by quality (ascending - lower quality first, matching game default)
                var qualityA = a.itemValue.Quality;
                var qualityB = b.itemValue.Quality;
                return qualityA.CompareTo(qualityB);
            });

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
