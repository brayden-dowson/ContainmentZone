﻿using InventorySystem.Items;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables;
using MEC;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheRiptide
{
    public class MenuItem
    {
        public ItemType item;
        public string description;
        public Func<Player, bool> on_click;

        public MenuItem(ItemType item, string description, Func<Player, bool> on_click)
        {
            this.item = item;
            this.description = description;
            this.on_click = on_click;
        }
    }

    public class Menu
    {
        public string description;
        public List<MenuItem> items;

        public Menu(string description, List<MenuItem> items)
        {
            this.description = description;
            this.items = items;
        }
    }

    public struct MenuInfo
    {
        public int total_items;
        public int broadcast_lines;
        public MenuInfo(int total_items, int broadcast_lines)
        {
            this.total_items = total_items;
            this.broadcast_lines = broadcast_lines;
        }
    }

    public class InventoryMenu
    {
        public static readonly InventoryMenu Singleton = new InventoryMenu();

        private Dictionary<int, int> player_menu = new Dictionary<int, int>();
        private Dictionary<int, Menu> menus = new Dictionary<int, Menu>();

        public void RegisterPlayer(Player player)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                player_menu.Add(player.PlayerId, 0);
        }

        public void UnregisterPlayer(Player player)
        {
            if (player_menu.ContainsKey(player.PlayerId))
                player_menu.Remove(player.PlayerId);
        }

        public bool OnPlayerDropitem(Player player, ItemBase item)
        {
            return SelectedMenuItem(player, item);
        }

        public bool OnPlayerChangedItem(Player player, ItemBase item)
        {
            if (!SelectedMenuItem(player, item))
            {
                player.CurrentItem = null;
                return false;
            }
            return true;
        }

        private bool SelectedMenuItem(Player player, ItemBase item)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                return true;
            bool allow = true;
            if (!menus.ContainsKey(player_menu[player.PlayerId]))
                return true;
            Menu menu = menus[player_menu[player.PlayerId]];
            Predicate<MenuItem> has_item = (menu_item) => { return menu_item.item == item.ItemTypeId; };
            if (menu.items.Exists(has_item))
                allow = menu.items.Find(has_item).on_click(player);
            BroadcastOverride.UpdateIfDirty(player);
            return allow;
        }

        //public bool OnPlayerUseItem(Player player, UsableItem item)
        //{
        //    if (!player_menu.ContainsKey(player.PlayerId))
        //        return true;
        //    if (player_menu[player.PlayerId] != 0)
        //        return false;
        //    return true;
        //}

        //public bool OnThrowItem(Player player, ItemBase item, Rigidbody rb)
        //{
        //    if (!player_menu.ContainsKey(player.PlayerId))
        //        return true;
        //    if (player_menu[player.PlayerId] != 0)
        //        return false;
        //    return true;
        //}

        //public bool OnPlayerThrowProjectile(Player player, ThrowableItem item, ThrowableItem.ProjectileSettings projectileSettings, bool fullForce)
        //{//todo fix
        //    if (!player_menu.ContainsKey(player.PlayerId))
        //        return true;
        //    if (player_menu[player.PlayerId] != 0)
        //        return false;
        //    return true;
        //}

        public void ResetMenuState(Player player)
        {
            if (!player_menu.ContainsKey(player.PlayerId))
                return;

            if (player_menu[player.PlayerId] != 0)
            {
                BroadcastOverride.ClearLines(player, BroadcastPriority.High);
                player_menu[player.PlayerId] = 0;
            }
        }

        public void CreateMenu(int id, string description, List<MenuItem> items)
        {
            menus.Add(id, new Menu(description, items));
        }

        public void ShowMenu(Player player, int menu_id)
        {
            SetMenu(player, menu_id);
            Menu menu = menus[menu_id];

            player.ClearInventory();
            BroadcastOverride.ClearLines(player, BroadcastPriority.High);
            List<string> broadcast = new List<string>();
            List<ItemType> items = new List<ItemType>();
            if (menu.description != "")
                broadcast.Add(menu.description);
            for (int i = 0; i < menu.items.Count(); i++)
            {
                if (menu.items[i].description != "")
                    broadcast.Add(menu.items[i].description);
                items.Add(menu.items[i].item);
            }
            if (!broadcast.IsEmpty())
            {
                if (broadcast.Count >= 7)
                    BroadcastOverride.SetEvenLineSizes(player, broadcast.Count() + 1);
                else
                    BroadcastOverride.SetEvenLineSizes(player, 7);
                BroadcastOverride.BroadcastLines(player, 1, 1500.0f, BroadcastPriority.High, broadcast);
            }

            int index = 0;
            Action add_items_inorder = null;
            add_items_inorder = () =>
            {
                player.AddItem(items[index]);
                index++;
                if (index < items.Count)
                {
                    Timing.CallDelayed(0.0f, add_items_inorder);
                }
            };
            Timing.CallDelayed(0.0f, add_items_inorder);
        }

        public void SetMenu(Player player, int menu_id)
        {
            player_menu[player.PlayerId] = menu_id;
        }

        public MenuInfo GetInfo(int menu_id)
        {
            Menu menu = menus[menu_id];
            int broadcast_lines = 0;
            if (menu.description != "")
                broadcast_lines++;
            foreach (MenuItem item in menu.items)
                if (item.description != "")
                    broadcast_lines++;
            return new MenuInfo(menu.items.Count, broadcast_lines);
        }

        public int GetPlayerMenuID(Player player)
        {
            return player_menu[player.PlayerId];
        }

        public void Clear()
        {
            menus.Clear();
            foreach (var id in player_menu.Keys.ToList())
                player_menu[id] = 0;
        }
    }
}