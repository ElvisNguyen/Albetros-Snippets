using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Albetros.Core;
using Albetros.Core.Database;
using Albetros.Core.Enum;
using Albetros.Game.Managers;
using Albetros.Game.Packet;
using Albetros.Core.Space;
using System.Windows.Forms;
using System.IO;

namespace Albetros.Game
{
    public class PacketHandler
    {
        public static List<uint> New_Role = new List<uint>();
        public Player user;
        public PacketHandler(Player _user)
        {
            user = _user;
        }
        public void Split(byte[] packet)
        {
            if (packet == null)
                return;
            if (user == null)
                return;
        roleAgain:
            ushort Length = BitConverter.ToUInt16(packet, 0);
        if ((Length + 8) == packet.Length)
            {
                Packets.WriteString("TQServer", (packet.Length - 8), packet);
                Handle(packet);
                return;
            }
        else if ((Length + 8) > packet.Length)
            {
                return;
            }
            else
            {
                byte[] Packet = new byte[(Length + 8)];
                Buffer.BlockCopy(packet, 0, Packet, 0, (Length + 8));
                byte[] _buffer = new byte[(packet.Length - (Length + 8))];
                Buffer.BlockCopy(packet, (Length + 8), _buffer, 0, (packet.Length - (Length + 8)));
                packet = _buffer;
                Packets.WriteString("TQServer", (Packet.Length - 8), Packet);
                Handle(Packet);
                goto roleAgain;
            }
        }
        public unsafe void Handle(byte[] packet)
        {
            try
            {
                if (!user.Client.Connected)
                    return;

                fixed (byte* ptr = packet)
                {
                    var type = *((ushort*)(ptr + 2));
                    switch (type)
                    {
                        #region 1001 Character creation
                        case 1001:
                            {
                                byte PacketType = packet[4];// packet reader 
                                switch (PacketType)
                                {
                                    case 0:
                                        {
                                            /// <summary>
                                            /// this is where the create button is clicked 
                                            /// </summary>
                                            #region Create
                                            try
                                            {
                                                CreatePlayerPacket msg = ptr;
                                                user.Name = msg.FirstName;
                                                if (!Kernel.ValidChars.IsMatch(user.Name) || user.Name.Length < 3 || user.Name.Length >= 16) 
                                                {
                                                    user.SendPopupMsg("Invalid name!");
                                                    break;
                                                }

                                                if (!(msg.Model == 1003 || msg.Model == 1004 || msg.Model == 2001 || msg.Model == 2002))
                                                {
                                                    user.SendPopupMsg("Invalid model!");
                                                    break;
                                                }

                                                if (!(msg.Job == 10 || msg.Job == 20 || msg.Job == 40 || msg.Job == 50 || msg.Job == 60 || msg.Job == 100))
                                                {
                                                    user.SendPopupMsg("Invalid job!");
                                                    break;
                                                }

                                                if (Database.NameExists(user.Name))
                                                {
                                                    user.SendPopupMsg("Name is in use!");
                                                    break;
                                                }
                                                if(user.Create(msg))
                                                    if (Database.CreatePlayer(user))
                                                {

//#warning It lets you log in but will not display the User Interface. Till it's fixed I'm gonna just disconnect them
                                                    if (user.Name != "Monkenstein")
                                                    {
                                                        user.Disconnect(false);
                                                        return;
                                                    }
                                                    System.Threading.Thread.Sleep(150);//delay to make sure database has finished updating
                                                    if (Database.PullLogin(user))
                                                    {
                                                        if (Kernel.Clients.ContainsKey(user.UID))
                                                            Kernel.Clients.Remove(user.UID);
                                                        Kernel.Clients.Add(user.UID, user);
                                                        Database.UpdateOnline();
                                                        var response = new TalkPacket("SYSTEM", "ALLUSERS", "ANSWER_OK", string.Empty, 0xffffff, ChatType.Register);
                                                        user.Send(response);
                                                        return;
                                                        user.Send(PlayerInfoPacket.Create(user));
                                                        if (user.Health < 1) user.Health = 1;
                                                        user.Send(DateTimePacket.Create());
                                                        UserManager.LoginUser(user);
                                                        user.GuildAttribute.Create();
                                                        Database.ModifyAccount(0, "Hash", user.UID); Database.ModifyAccount(0, "Timestamp", user.UID);
                                                    }
                                                    // New_Role.Add(user.UID);// threadsafe ? no
                                                    // var response = new TalkPacket("SYSTEM", "ALLUSERS", "ANSWER_OK", string.Empty, 0xffffff, ChatType.Register);
                                                    // user.Send(response);
                                                    else
                                                        Console.WriteLine("Failed loading justmade char");

                                                }
                                                else
                                                    Console.WriteLine("Failed making char");
                                            }
                                            catch (Exception X) { Console.WriteLine(X); }
                                            break;
                                            #endregion
                                        }
                                    case 1:
                                        {
                                            /// <summary>
                                            /// Disconnect/Remove the client 
                                            /// </summary>
                                            #region Disconnect if the client Clicked the back button
                                            if (Kernel.Clients.ContainsKey(user.UID)) { Kernel.Clients[user.UID].Disconnect(true); user.Client.Disconnect(false); return; }
                                            break;
                                            #endregion
                                        }
                                }
                                break;
                            }
//                        case 1001: // character creation
//                            {
//                                CreatePlayerPacket msg = ptr;
//                                if (!new Regex("^[a-zA-Z0-9]{4,16}$").IsMatch(msg.FirstName)) // TODO: replace with _static_ regular expression
//                                {
//                                    user.SendPopupMsg("Invalid name!");
//                                    break;
//                                }

//                                if (!(msg.Model == 1003 || msg.Model == 1004 || msg.Model == 2001 || msg.Model == 2002))
//                                {
//                                    user.SendPopupMsg("Invalid model!");
//                                    break;
//                                }

//                                if (!(msg.Job == 10 || msg.Job == 20 || msg.Job == 40 || msg.Job == 50 || msg.Job == 60 || msg.Job == 100))
//                                {
//                                    user.SendPopupMsg("Invalid job!");
//                                    break;
//                                }

//                                if (Database.NameExists(msg.FirstName))
//                                {
//                                    user.SendPopupMsg("Name is in use!");
//                                    break;
//                                }

//                                if (user.Create(msg))
//                                {
//                                    if (Database.CreatePlayer(user))
//                                    {
//#warning CODE TO LOGIN AFTER CHARACTER CREATION GOES HERE
//                                        //user.DelayedActions.AddAction(DelayedActionType.ContinueLogin, new Action(() => user.Login()), 1000);
//                                        //Need to figure out what packet we are missing. Right now I'm sending Hero info, map enter, datetime. (the delay was because there was time gap between adding to db and being able to pull
//                                        //If it loads all data correctly (delay is added such as this), it logs in fine but shows no game interface. There must be another packet/subtype somewhere.

//                                        user.JustCreated = true;
//                                        user.Disconnect(false);//Should be able to just send Answer_OK and hero info packet to continue login!
//                                    }
//                                    else
//                                    {
//                                        Kernel.WriteLine("Failed to create character in database: id[{0}] name[{1}]",
//                                                         user.UID, user.Name);
//                                        user.Disconnect(false);
//                                    }
//                                }

//                                break;
//                            }
                        #endregion
                        #region 1004: Chat
                        case 1004:
                            {
                                TalkPacket received = ptr;
                                if (received.Words.StartsWith("/"))
                                {
                                    Handler.HandleCommand(user, received.Words.Split(' '));
                                    break;
                                }

                                if (user.Health == 0 && received.Type != ChatType.Team) // ghost
                                {
                                    received.Type = ChatType.Ghost;
                                    user.SendToScreen(received, false);
                                    return;
                                }

                                switch (received.Type)
                                {
                                    case ChatType.Talk:
                                        {
                                            user.SendToScreen(received, false);
                                            break;
                                        }
                                    case ChatType.World:
                                        {
                                            Kernel.SendToServer(received, user);
                                            Kernel.SendIrcMessage("#HellmouthRevival", "{0}: {1}", received.Speaker, received.Words);
                                            user.NextWorldMsg = Native.timeGetTime().AddSeconds(15);
                                            Database.LogChat(received);
                                            break;
                                        }
                                    case ChatType.HawkMessage:
                                        if (user.Shop != null && user.Shop.Vending)
                                        { user.Shop.HawkMsg = received; user.SendToScreen(received, true); }
                                        break;
                                    case ChatType.Whisper:
                                        {
                                            foreach (Player role in Kernel.Clients.Values)
                                                if (role.Name.ToLower() == received.Hearer.ToLower())
                                                {
                                                    received.HearerLookface = role.Mesh;
                                                    received.SpeakerLookface = user.Mesh;
                                                    Database.LogChat(received);
                                                    role.Send(received);
                                                    break;
                                                }
                                            break;
                                        }
                                    case ChatType.Friend:
                                        {
                                            user.SendToFriends(received);
                                            break;
                                        }
                                    case ChatType.Team:
                                        {
                                            if (user.Team != null)
                                                foreach (Player role in user.Team.Members)
                                                    role.Send(received);
                                            else if (user.EventTeam != null)
                                                foreach (Player role in user.EventTeam.Members)
                                                {
                                                    role.Send(received);
                                                }
                                            break;
                                        }
                                    case ChatType.Guild:
                                        {
                                            var guildId = user.GuildId;
                                            var guild = GuildManager.GetGuild(guildId);
                                            if (guild != null)
                                            {
                                                guild.BroadcastGuildMsg(received, user);
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            Kernel.WriteLine("unhandled chat type: " + received.Type);
                                            break;
                                        }
                                }
                                break;
                            }
                        #endregion
                        #region 1009: item usage/ping
                        case 1009:
                            {
                                ItemPacket item = ptr;
                                switch (item.Action)
                                {
                                    #region Activate Weapon Accessory
                                    case ItemAction.ActivateAccessory:
                                        user.Send(item);
                                        break;
                                    #endregion
                                    #region Player Shops
                                    #region Add to Booth
                                    #region Gold Item
                                    case ItemAction.BoothAdd:
                                        {
                                            if (user.Shop == null || !user.Shop.Vending)
                                                return;
                                            if (user.Shop.Items.ContainsKey(item.Id))
                                                return;
                                            if (user.Inventory.ContainsKey(item.Id))
                                            {
                                                var toAdd = user.Inventory[item.Id];
                                                var saleItem = new SaleItem(toAdd, item.Data1, false);
                                                user.Shop.Items.Add(toAdd.UniqueID, saleItem);
                                                user.Send(item);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region CP Item
                                    case ItemAction.BoothAddCP:
                                        {
                                            if (user.Shop == null || !user.Shop.Vending)
                                                return;
                                            if (user.Shop.Items.ContainsKey(item.Id))
                                                return;
                                            if (user.Inventory.ContainsKey(item.Id))
                                            {
                                                var toAdd = user.Inventory[item.Id];
                                                var saleItem = new SaleItem(toAdd, item.Data1, true);
                                                user.Shop.Items.Add(toAdd.UniqueID, saleItem);
                                                user.Send(item);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #endregion
                                    #region Remove From Booth
                                    case ItemAction.BoothDelete:
                                        if (user.Shop != null && user.Shop.Vending)
                                        {
                                            if (user.Shop.Items.ContainsKey(item.Id))
                                            {
                                                user.Shop.Items.Remove(item.Id);
                                                user.Send(item);
                                            }
                                        }
                                        break;
                                    #endregion
                                    #region Buy From Booth
                                    case ItemAction.BoothBuy:
                                        {
                                            if (user.Inventory.Count > 39)
                                                return;
                                            var pts = user.Map.QueryScreen(user);
                                            foreach (var o in pts)
                                            {
                                                if (o is Player)
                                                {
                                                    var role = o as Player;
                                                    if (role == null)
                                                        continue;
                                                    if (role.Shop == null || role.Shop.Carpet == null)
                                                        continue;
                                                    if (!role.Shop.Items.ContainsKey(item.Id))
                                                        continue;
                                                    if (!role.Inventory.ContainsKey(item.Id))
                                                        continue;
                                                    bool bought = false;
                                                    //select item using what?...
                                                    var vI = role.Shop.Items[item.Id];
                                                    if (vI.CpCost)
                                                    {
                                                        if (user.CP < vI.Price)
                                                            return;
                                                        user.CP -= vI.Price;
                                                        role.CP += vI.Price;
                                                        bought = true;
                                                    }
                                                    else
                                                    {
                                                        if (user.Money < vI.Price)
                                                            return;
                                                        user.Money -= vI.Price;
                                                        role.Money += vI.Price;
                                                        bought = true;
                                                    }
                                                    if (bought)
                                                    {
                                                        user.Inventory.Add(vI.Item.UniqueID, vI.Item);
                                                        user.Send(Packet.ItemInfoPacket.Create(vI.Item, 1));
                                                        user.Send(item);


                                                        item.Action = ItemAction.BoothDelete;
                                                        role.Send(item);
                                                        item.Action = ItemAction.Remove;
                                                        role.Send(item);

                                                        role.Send(new Packet.TalkPacket(ChatType.System, user.Name + " has purchased your " + vI.Item.ItemtypeData.Value.Name + " for " + vI.Price + (vI.CpCost ? "CPs" : "Silvers")));

                                                        role.Shop.Items.Remove(vI.Item.UniqueID);
                                                        role.Inventory.Remove(vI.Item.UniqueID);
                                                        Database.ModifyItem(user.UID, "Owner", vI.Item.UniqueID);
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Request Player Shop
                                    case ItemAction.BoothQuery:
                                        {
                                            var pts = user.Map.QueryScreen(user);
                                            foreach (var o in pts)
                                            {
                                                if (o is Player)
                                                {
                                                    var role = o as Player;
                                                    if (role != null && role.Shop != null && role.Shop.Carpet != null && role.Shop.Carpet.UID == item.Id)
                                                    {
                                                        foreach (SaleItem si in role.Shop.Items.Values)
                                                        {
                                                            if (!role.Inventory.ContainsKey(si.Item.UniqueID))
                                                            { role.Shop.Items.Remove(si.Item.UniqueID); break; }
                                                            var z = Packet.ViewItem.Create(item.Id, si.Item);
                                                            z.Price = si.Price;
                                                            if (si.CpCost)
                                                                z.ViewType = 3;
                                                            else z.ViewType = 1;
                                                            user.Send(z);
                                                            
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    #endregion
                                    #endregion
                                    #region Warehouses
                                    #region Check Warehouse Money
                                    case ItemAction.QueryMoneySaved:
                                        {
                                            item.Data1 = user.WarehouseMoney;
                                            user.Send(item);
                                            break;
                                        }
                                    #endregion
                                    #region Add Warehouse Money
                                    case ItemAction.SaveMoney:
                                        {
                                            if (user.Money < item.Data1)
                                                return;
                                            user.Money -= item.Data1;
                                            user.WarehouseMoney += item.Data1;
                                            Database.ModifyCharacter(user.Money, "Money", user.UID);
                                            Database.ModifyCharacter(user.WarehouseMoney, "WhMoney", user.UID);
                                            break;
                                        }
                                    #endregion
                                    #region Remove Warehouse Money
                                    case ItemAction.DrawMoney:
                                        {
                                            if (user.WarehouseMoney < item.Data1)
                                                return;
                                            user.WarehouseMoney -= item.Data1;
                                            user.Money += item.Data1;
                                            Database.ModifyCharacter(user.Money, "Money", user.UID);
                                            Database.ModifyCharacter(user.WarehouseMoney, "WhMoney", user.UID);
                                            break;
                                        }
                                    #endregion
                                    #endregion
                                    #region Repair All
                                    case ItemAction.RepairAll:
                                        {
                                            break;
                                        }
                                    #endregion
                                    #region Repair Item
                                    case ItemAction.Repair:
                                        {
                                            if (!user.Inventory.ContainsKey(item.Id))
                                                return;
                                            Structures.ItemInfo torepair = user.Inventory[item.Id];
                                            if (torepair.Suspicious)
                                                return;
                                            if (Calculations.Items.IsArrow(torepair.StaticID))
                                                return;
                                            if (torepair.CurrentDura > 0 && torepair.CurrentDura < torepair.MaximumDura)
                                            {

                                                uint cost = torepair.ItemtypeData.Value.Price;
                                                uint duraCost = (torepair.CurrentDura * cost) / torepair.MaximumDura;
                                                cost -= duraCost;
                                                if (user.Money < cost)
                                                    return;
                                                torepair.CurrentDura = torepair.MaximumDura;
                                                Database.ModifyItem(torepair.CurrentDura, "Dura", torepair.UniqueID);
                                                user.Send(Packet.ItemInfoPacket.Create(torepair, 3));
                                            }
                                            else if (torepair.CurrentDura == 0)
                                            {
                                                if (user.HasItem(1088001, 5))
                                                {
                                                    user.RemoveItem(1088001, 5);
                                                    torepair.CurrentDura = torepair.MaximumDura;
                                                    Database.ModifyItem(torepair.CurrentDura, "Dura", torepair.UniqueID);
                                                    user.Send(Packet.ItemInfoPacket.Create(torepair, 3));
                                                }
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region DB Upgrade
                                    case ItemAction.Improve:
                                        {
                                            if (!user.Alive)
                                                return;
                                            if (!user.Inventory.ContainsKey(item.Id))
                                                return;
                                            if (!user.Inventory.ContainsKey(item.Data1))
                                                return;
                                            Structures.ItemInfo toUpgrade = user.Inventory[item.Id];
                                            Structures.ItemInfo upgrader = user.Inventory[item.Data1];
                                            if (upgrader.StaticID != 1088000)//db
                                                return;
                                            if (toUpgrade.StaticID % 10 == 9)//already super
                                                return;
                                            user.RemoveItem(upgrader);
                                            uint newID = toUpgrade.StaticID;
                                            if (newID % 10 < 5)
                                                newID += 5 - newID % 10;
                                            newID += 1;
                                            if (Calculations.Items.TypeOfItem(newID) != Calculations.Items.TypeOfItem(toUpgrade.StaticID))
                                                return;
                                            if (Calculations.Items.LevelOfItem(newID) != Calculations.Items.LevelOfItem(toUpgrade.StaticID))
                                                return;
                                            if (!Calculations.Damage.PercentSuccess(Calculations.Items.QualityChance(toUpgrade.StaticID)))                                               
                                                {
                                                    toUpgrade.CurrentDura /= 2;
                                                    Database.ModifyItem(toUpgrade.CurrentDura, "Dura", toUpgrade.UniqueID);
                                                    user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                    user.SendMessage("SYSTEM", user.Name, "Failure! Your item failed to upgrade", uint.MaxValue, ChatType.System);
                                                    return;
                                                }
                                            if (Kernel.Itemtypes.ContainsKey(newID))
                                            {
                                                if (toUpgrade.Gem1 == 0 && Calculations.Damage.PercentSuccess(1.5))
                                                {
                                                    toUpgrade.Gem1 = 255;
                                                    Database.ModifyItem(255, "Soc1", toUpgrade.UniqueID);
                                                    Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " is so lucky to make the first socket in his/her " + toUpgrade.ItemtypeData.Value.Name));
                                                }
                                                else if (toUpgrade.Gem2 == 0 && Calculations.Damage.PercentSuccess(.9))
                                                {
                                                    toUpgrade.Gem1 = 255;
                                                    Database.ModifyItem(255, "Soc2", toUpgrade.UniqueID);
                                                    Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " is so lucky to make the second socket in his/her " + toUpgrade.ItemtypeData.Value.Name));
                                                }
                                                toUpgrade.StaticID = newID;
                                                toUpgrade.ItemtypeData = Kernel.Itemtypes[newID];
                                                Database.ModifyItem(toUpgrade.StaticID, "ID", toUpgrade.UniqueID);
                                                user.Inventory[toUpgrade.UniqueID] = toUpgrade;
                                                user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                user.SendMessage("SYSTEM", user.Name, "SUCCESS! Your item's quality has been upgraded", uint.MaxValue, ChatType.System); return;
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Meteor Upgrade
                                    case ItemAction.Uplev:
                                        {
                                            if (!user.Alive)
                                                return;
                                            if (!user.Inventory.ContainsKey(item.Id))
                                                return;
                                            if (!user.Inventory.ContainsKey(item.Data1))
                                                return;
                                            Structures.ItemInfo toUpgrade = user.Inventory[item.Id];
                                            Structures.ItemInfo upgrader = user.Inventory[item.Data1];
                                            if (upgrader.StaticID != 1088001 && upgrader.StaticID != 1088002)//met or met tear
                                                return;
                                            if (toUpgrade.ItemtypeData.Value.Requirements.Level >= 120)
                                            { user.SendMessage("SYSTEM", user.Name, "Cannot upgrade past level 120.", uint.MaxValue, ChatType.System); return; }
                                            user.RemoveItem(upgrader);
                                            if (!Calculations.Damage.PercentSuccess(Calculations.Items.Chance(toUpgrade.StaticID)))
                                            {
                                                toUpgrade.CurrentDura /= 2;
                                                Database.ModifyItem(toUpgrade.CurrentDura, "Dura", toUpgrade.UniqueID);
                                                user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                user.SendMessage("SYSTEM", user.Name, "Failure! Your item failed to upgrade", uint.MaxValue, ChatType.System);
                                                return;
                                            }
                                            uint newID = toUpgrade.StaticID + 10;
                                            int loop = 4;
                                            while (!Kernel.Itemtypes.ContainsKey(newID))
                                            {
                                                newID += 10;
                                                loop--;
                                                if (loop <= 0)
                                                    break;
                                            }
                                            if (Calculations.Items.TypeOfItem(newID) != Calculations.Items.TypeOfItem(toUpgrade.StaticID))
                                                return;
                                            if (Calculations.Items.TypeOfItem(newID) == Calculations.Items.TypeOfItem(toUpgrade.StaticID))
                                            {
                                                toUpgrade.StaticID = newID;
                                                if (toUpgrade.Gem1 == 0 && Calculations.Damage.PercentSuccess(.5))
                                                {
                                                    Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " is so lucky to make the first socket in his/her " + toUpgrade.ItemtypeData.Value.Name));
                                                    toUpgrade.Gem1 = 255;
                                                    Database.ModifyItem(255, "Soc1", toUpgrade.UniqueID);
                                                }
                                                else if (toUpgrade.Gem2 == 0 && Calculations.Damage.PercentSuccess(.3))
                                                {
                                                    toUpgrade.Gem1 = 255;
                                                    Database.ModifyItem(255, "Soc2", toUpgrade.UniqueID);
                                                    Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " is so lucky to make the second socket in his/her " + toUpgrade.ItemtypeData.Value.Name));
                                                }
                                                toUpgrade.ChangeItemtype(newID);
                                                user.Inventory[toUpgrade.UniqueID] = toUpgrade;
                                                user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                user.SendMessage("SYSTEM", user.Name, "Sucecess! Your item's level has been upgraded", uint.MaxValue, ChatType.System);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Request Item Tooltip
                                    case ItemAction.RequestItemTooltip:
                                        {
                                            //user.Send(Database.PullItemTooltip(item.Id));
                                            break;
                                        }
                                    #endregion
                                    #region Shops
                                    #region Buy from Shop
                                    case ItemAction.Buy:
                                        if (!user.Alive)
                                            return;
                                        Handler.BuyFromShop(item, user);
                                        break;
                                    #endregion
                                    #region Sell to Shop
                                    case ItemAction.Sell:
                                        if (!user.Alive)
                                            return;
                                        Handler.SellToShop(item, user);
                                        break;
                                    #endregion
                                    #endregion
                                    #region Unequip Item
                                    case ItemAction.Unequip:
                                        {
                                            if (!user.Alive)
                                                return;
                                            Structures.ItemInfo z = user.Equipment.GetItemBySlot((ItemLocation)item.Data1);
                                            if (z != null)
                                            {
                                                if (z == user._disguiseGarment)
                                                    return;
                                                user.Equipment.UnequipItem((ItemLocation)item.Data1);
                                               // z.Unequip(user);
                                               // user.Recalculate();
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Use/Equip Item
                                    case ItemAction.Use:
                                        {
                                            if (!user.Alive)
                                                return;
                                            if (user.Map.ID == 2060)
                                                return;
                                            if (user.Map.ID == 700)
                                                return;
                                            ItemLocation equipTo = (ItemLocation)item.Data1;
                                            if (!user.Inventory.ContainsKey(item.Id))
                                                break;
                                            var itm = user.Inventory[item.Id];
                                            if (equipTo == ItemLocation.WeaponL)
                                            {
                                                //We want to eq to second hand.
                                                if (Calculations.Items.IsOneHand(itm.StaticID))//is one hand wep in OFFHAND! Only if ninja/tro/monk AND promoted past 40
                                                    if (user.ProfessionSort == ProfessionSort.Trojan || user.ProfessionSort == ProfessionSort.Monk || user.ProfessionSort == ProfessionSort.Ninja)
                                                    {
                                                        if (user.ProfessionLevel < 2)
                                                            return;
                                                    }
                                                    else
                                                        return;
                                                if (Calculations.Items.IsTwoHand(itm.StaticID))
                                                    return;
                                            }
                                            if (equipTo != ItemLocation.Inventory && equipTo <= ItemLocation.ALTSteed && Calculations.Items.ItemPosition(itm.StaticID) > ItemLocation.Inventory && Calculations.Items.CanEquip(user, itm))
                                                user.Equipment.EquipItem(user.Inventory[item.Id], equipTo);                                              
                                            else
                                                Handler.UseItem(item, user);
                                            break;
                                        }
                                    #endregion
                                    #region Drop Item
                                    case ItemAction.Drop:
                                        if (user.Inventory.ContainsKey(item.Id))
                                        {
                                            if (!user.Alive)
                                                return;
                                            Structures.ItemInfo dropItem = user.Inventory[item.Id];
                                            if (dropItem.Suspicious || dropItem.Locked )                                            
                                                return;
                                            if (dropItem.Free)
                                            {
                                                user.RemoveItem(dropItem);
                                                break;
                                            }
                                            
                                                Point toDrop = Calculations.Movement.FindItemLoc(user);
                                                GroundItem gi = new GroundItem(dropItem, (ushort) toDrop.X, (ushort) toDrop.Y, Kernel.Maps[user.Map.ID],0);
                                                user.SendToScreen(Packet.GroundItem.Create(gi, 1), true);                                            
                                            user.RemoveItem(dropItem, user.Inventory[item.Id].Amount);
                                        }
                                        break;
                                    #endregion
                                    #region Ping
                                    case ItemAction.Ping:
                                        user.Send(item);
                                        break;
                                    #endregion
                                    #region Socket Equipment (market)
                                    case ItemAction.SocketEquipment:
                                        {
                                            if (!user.Inventory.ContainsKey(item.Id))
                                                return;
                                            Structures.ItemInfo toUpgrade = user.Inventory[item.Id];
                                            if (toUpgrade.StaticID < 410003 || toUpgrade.StaticID > 610439)
                                            {
                                                user.SendMessage("SYSTEM", user.Name, "You can only socket weapons!", uint.MaxValue, ChatType.MessageBox);
                                                return;
                                            }
                                            if (toUpgrade.Gem1 == 0 && toUpgrade.Gem2 == 0)
                                            {
                                                if (!user.HasItem(1088000))
                                                {
                                                    user.SendMessage("SYSTEM", user.Name, "You must have at least one DragonBall!", uint.MaxValue, ChatType.MessageBox);
                                                    return;
                                                }
                                                user.RemoveItem(1088000, 1);
                                                toUpgrade.Gem1 = 255;
                                                Database.ModifyItem(toUpgrade.Gem1, "Soc1", toUpgrade.UniqueID);
                                                user.Inventory[toUpgrade.UniqueID] = toUpgrade;
                                                user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                user.SendMessage("SYSTEM", user.Name, "You have added your first socket successfully!", uint.MaxValue, ChatType.MessageBox);
                                            }
                                            else if (toUpgrade.Gem2 == 0)
                                            {
                                                if (!user.HasItem(1088000, 5))
                                                {
                                                    user.SendMessage("SYSTEM", user.Name, "You must have at least five DragonBalls!", uint.MaxValue, ChatType.MessageBox);
                                                    return;
                                                }
                                                user.RemoveItem(1088000, 5);
                                                toUpgrade.Gem2 = 255;
                                                Database.ModifyItem(toUpgrade.Gem2, "Soc2", toUpgrade.UniqueID);
                                                user.Inventory[toUpgrade.UniqueID] = toUpgrade;
                                                user.Send(Packet.ItemInfoPacket.Create(toUpgrade, 3));
                                                user.SendMessage("SYSTEM", user.Name, "You have added a second socket successfully!", uint.MaxValue, ChatType.MessageBox);
                                            }
                                            else
                                            {
                                                user.SendMessage("SYSTEM", user.Name, "This item already has dual sockets!", uint.MaxValue, ChatType.MessageBox);
                                                return;
                                            }

                                            user.Send(item);
                                            break;
                                        }
                                    #endregion
                                    #region DEFAULT
                                    default:
                                        Kernel.WriteLine("unknown 1009 subtype: " + item.Action);
                                        break;
                                    #endregion
                                }
                                break;
                            }
                        #endregion
                        #region 1015: String Packet
                        case 1015:
                            {
                                StringPacket incoming = ptr;
                                switch (incoming.Action)
                                {
                                    case StringAction.QueryMate://Spouse name request. Used in view gear and that's about it lol!
                                        {
                                            var target = user.Map.Search<Player>(incoming.Data);
                                            if (target == null)
                                                break;
                                            incoming.Strings.SetString(0, target.Spouse);
                                            user.Send(incoming);
                                            break;
                                        }
                                    case StringAction.WhisperWindowInfo:
                                        {
                                            Player role = null;
                                            foreach (Player p in Kernel.Clients.Values)
                                                if (p.Name == incoming.Strings.GetString(0))
                                                {
                                                    role = p;
                                                    break;
                                                }
                                            if (role != null)
                                            {
                                                string toAdd = role.UID + " ";
                                                toAdd += role.Level + " ";
                                                toAdd += role.Level + " ";//battle power
                                                toAdd += "# ";//unknown
                                                toAdd += "# ";//unknown
                                                toAdd += role.Spouse + " ";
                                                toAdd += 0 + " ";//unknown
                                                if (role.Mesh % 10 < 3)
                                                    toAdd += "1 ";
                                                else
                                                    toAdd += "0 ";
                                                incoming.Strings.AddString(toAdd);
                                                user.Send(incoming);
                                            }

                                            break;
                                        }
                                    default:
                                        Kernel.WriteLine("Unhandled string request type: {0} from {1}", incoming.Action, user.Name); break;
                                }
                            }
                            break;
                        #endregion
                        #region 1019: Friends

                        case 1019:
                            {
                                if (!user.Alive) break;

                                FriendPacket receive = ptr;
                                switch (receive.Action)
                                {
                                    case FriendAction.Apply:
                                        {
                                            Player target;
                                            Kernel.Clients.TryGetValue(receive.FriendId, out target);
                                            if (target == null) break;

                                            if (Calculations.Movement.Distance(user, target) > 18) break;

                                            if (user.GetFriendAmount() >= 50)
                                            {
                                                user.SendSystemMsg("The target's friend list is full.");
                                                break;
                                            }

                                            if (user.GetFriend(receive.FriendId) != null)
                                            {
                                                user.SendSystemMsg("The target has been your friend.");
                                                break;
                                            }

                                            if (target.FetchApply(ApplyType.Friend) != user.UID)
                                            {
                                                user.SetApply(ApplyType.Friend, receive.FriendId);

                                                target.SendSystemMsg("{0} wishes to be friends with you!", user.Name);
                                                user.SendSystemMsg("Request of making friends has been sent out.");
                                            }
                                            else
                                            {
                                                if (user.AddFriend(receive.FriendId, receive.Name) && target.AddFriend(user.UID, user.Name))
                                                {
                                                    user.Send(FriendPacket.Create(FriendAction.GetInfo, target.UID, target.Name, true));
                                                    target.Send(FriendPacket.Create(FriendAction.GetInfo, user.UID, user.Name, true));

                                                    var msg = string.Format("{0} and {1} are friends from now on!", user.Name, target.Name);
                                                    user.SendToScreen(msg, true);
                                                }
                                            }
                                            break;
                                        }
                                    case FriendAction.Break:
                                        {
                                            Player target;
                                            Kernel.Clients.TryGetValue(receive.FriendId, out target);

                                            var friend = user.GetFriend(receive.FriendId);
                                            if (friend == null)
                                            {
                                                if (target != null)
                                                {
                                                    user.SendSystemMsg("{0} is not your friend.", target.Name);
                                                }
                                                break;
                                            }

                                            var friendName = friend.FriendName;
                                            if (user.DeleteFriend(receive.FriendId))
                                            {
                                                var msg = string.Format("{0} broke up friendship with {1}.", user.Name, friendName);
                                                user.SendToScreen(msg, true);

                                                user.Send(packet);
                                            }

                                            if (target != null)
                                            {
                                                if (target.DeleteFriend(user.UID))
                                                {
                                                    target.Send(FriendPacket.Create(FriendAction.Break, user.UID, user.Name));
                                                }
                                            }
                                            else
                                            {
                                                ServerDatabase.Context.Friends.DeleteFriend(receive.FriendId, user.UID);
                                            }
                                            break;
                                        }
                                }

                                break;
                            }

                        #endregion
                        #region 1022: Interaction (melee/magic/interact)
                        case 1022:
                            {
                                if (user.StatusSet.ContainsKey(StatusType.ReviveProtection))
                                    user.DetachStatus(StatusType.ReviveProtection);
                                if (user.ContainsFlag1(Effect1.Riding))
                                    user.RemoveFlag1(Effect1.Riding);
                                user.Action = ConquerAction.None;
                                InteractPacket receive = ptr;
                                switch (receive.Action)
                                {
                                    case InteractAction.Court:
                                        {

                                            var role = user.Map.Search<Player>(receive.TargetId);
                                            if (role == null)
                                                return;

                                            if (user.Spouse != "None")
                                            {
                                                user.SendMessage("SYSTEM", user.Name, " you already have a spouse!", uint.MaxValue, ChatType.Center);
                                                return;
                                            }
                                            if (role.Spouse != "None")
                                            {
                                                user.SendMessage("SYSTEM", user.Name, role.Name + " already has a spouse!", uint.MaxValue, ChatType.Center);
                                                return;
                                            }
                                            role.Send(receive);
                                            break;
                                        }
                                    case InteractAction.Marry:
                                        {
                                            var role = user.Map.Search<Player>(receive.TargetId);
                                            if (role == null)
                                                return;

                                            if (user.Spouse != "None")
                                            {
                                                user.SendMessage("SYSTEM", user.Name, " you already have a spouse!", uint.MaxValue, ChatType.Center);
                                                return;
                                            }
                                            if (role.Spouse != "None")
                                            {
                                                user.SendMessage("SYSTEM", user.Name, role.Name + " already has a spouse!", uint.MaxValue, ChatType.Center);
                                                return;
                                            }

                                            Kernel.SendToServer(new Packet.TalkPacket("SYSTEM", "", user.Name + " and " + role.Name + " have been united in marriage!", "", System.Drawing.Color.Azure, ChatType.Center));
                                            user.Spouse = role.Name;
                                            Database.ModifyCharacter(user.Spouse, "Spouse", user.UID);
                                            user.Send(Packet.StringPacket.Create(StringAction.Mate, user.Spouse, user.UID));
                                            role.Spouse = user.Name;
                                            Database.ModifyCharacter(role.Spouse, "Spouse", role.UID);
                                            role.Send(Packet.StringPacket.Create(StringAction.Mate, role.Spouse, role.UID));
                                            user.SendToScreen(Packet.StringPacket.Create(StringAction.Fireworks, "", user.UID), true);

                                            break;
                                        }

                                    case InteractAction.Attack:
                                    case InteractAction.MagicAttack:
                                    case InteractAction.Shoot:
                                        {
                                            if (!user.ContainsFlag1(Effect1.Vortex)) 
                                            user.AttackPacket = null;
                                            if (receive.Action == InteractAction.MagicAttack)
                                            {
                                                #region TemporaryDecryption
                                                ushort SkillId = Convert.ToUInt16(((long)packet[24] & 0xFF) | (((long)packet[25] & 0xFF) << 8));
                                                SkillId ^= (ushort)0x915d;
                                                SkillId ^= (ushort)user.UID;
                                                SkillId = (ushort)(SkillId << 0x3 | SkillId >> 0xd);
                                                SkillId -= 0xeb42;

                                                uint Target = ((uint)packet[12] & 0xFF) | (((uint)packet[13] & 0xFF) << 8) | (((uint)packet[14] & 0xFF) << 16) | (((uint)packet[15] & 0xFF) << 24);
                                                Target = ((((Target & 0xffffe000) >> 13) | ((Target & 0x1fff) << 19)) ^ 0x5F2D2463 ^ user.UID) - 0x746F4AE6;

                                                ushort TargetX = 0;
                                                ushort TargetY = 0;
                                                long xx = (packet[16] & 0xFF) | ((packet[17] & 0xFF) << 8);
                                                long yy = (packet[18] & 0xFF) | ((packet[19] & 0xFF) << 8);
                                                xx = xx ^ (user.UID & 0xffff) ^ 0x2ed6;
                                                xx = ((xx << 1) | ((xx & 0x8000) >> 15)) & 0xffff;
                                                xx |= 0xffff0000;
                                                xx -= 0xffff22ee;
                                                yy = yy ^ (user.UID & 0xffff) ^ 0xb99b;
                                                yy = ((yy << 5) | ((yy & 0xF800) >> 11)) & 0xffff;
                                                yy |= 0xffff0000;
                                                yy -= 0xffff8922;
                                                TargetX = Convert.ToUInt16(xx);
                                                TargetY = Convert.ToUInt16(yy);
                                                // receive.TargetId = Target;
                                                receive.MagicType = SkillId;
                                                receive.PositionX = TargetX;
                                                receive.PositionY = TargetY;
                                                #endregion

                                            }
                                            if (user.Alive)
                                                AttackProcessor.ProcessAttackPacket(receive, user);
                                            break;
                                        }
                                    case InteractAction.CounterKillSwitch:
                                        {
                                            if (receive.SenderId != user.UID)
                                                return;
                                            if (user.CounterKill)
                                            {
                                                user.CounterKill = !user.CounterKill;
                                                receive.Damage = 0;
                                                user.Send(receive);
                                                user.LastCounter = DateTime.Now;
                                            }
                                            else if (user.Stamina > 99)
                                            {
                                                user.Stamina -= 100;
                                                user.CounterKill = true;
                                                receive.Damage = 1;
                                                user.Send(receive);
                                                user.LastCounter = DateTime.Now;
                                            }
                                            break;
                                        }
                                    case InteractAction.InteractRequest:
                                        {
                                            var obj = user.Map.Search<Player>(receive.TargetId);
                                            if (obj == null) return;

                                            obj.Send(receive);

                                            break;
                                        }
                                    case InteractAction.InteractConfirm:
                                        {
                                            user.SendToScreen(receive, false);
                                            break;
                                        }
                                    case InteractAction.InteractUnknown:
                                        {
                                            user.Send(receive);
                                            var obj = user.Map.Search<Player>(receive.TargetId);
                                            if (obj == null) return;
                                            receive.SenderId = receive.TargetId;
                                            receive.TargetId = user.UID;
                                            obj.Send(receive);
                                            break;
                                        }
                                    case InteractAction.InteractStop:
                                        {
                                            user.Send(receive);
                                            var obj = user.Map.Search(receive.TargetId);
                                            if (obj == null) return;
                                            var entity = obj as Player;
                                            if (entity == null) return;
                                            receive.SenderId = receive.TargetId;
                                            receive.TargetId = user.UID;
                                            entity.Send(receive);
                                            break;
                                        }
                                    default:
                                        {
                                            Kernel.WriteLine("Unknown interaction type: " + receive.Action);

                                            break;
                                        }
                                }
                                break;
                            }

                        #endregion
                        #region 1027 Socket Gem
                        case 1027:
                            {
                                uint ItemID = BitConverter.ToUInt32(packet, 8);
                                uint GemID = BitConverter.ToUInt32(packet, 12);
                                byte socket = (byte)BitConverter.ToUInt16(packet, 16);
                                byte subtype = packet[18];
                                if (!user.Inventory.ContainsKey(ItemID))
                                    return; Structures.ItemInfo main = user.Inventory[ItemID];
                                if (subtype == 0)//add gem
                                {
                                    if (!user.Inventory.ContainsKey(GemID))
                                        return;
                                    Structures.ItemInfo minor = user.Inventory[GemID];
                                    if (socket == 1)
                                    {
                                        if (main.Gem1 != 255)
                                            return;
                                        main.Gem1 = (byte)(minor.StaticID % 1000);
                                        Database.ModifyItem(main.Gem1, "Soc1", main.UniqueID);
                                        user.RemoveItem(minor);
                                        user.Send(Packet.ItemInfoPacket.Create(main, 3));
                                    }
                                    else
                                    {
                                        if (main.Gem2 != 255)
                                            return;
                                        main.Gem2 = (byte)(minor.StaticID % 1000);
                                        Database.ModifyItem(main.Gem2, "Soc2", main.UniqueID);
                                        user.RemoveItem(minor);
                                        user.Send(Packet.ItemInfoPacket.Create(main, 3));
                                    }
                                }
                                else if (subtype == 1) //remove gem
                                {
                                    if (socket == 1)
                                    {
                                        if (main.Gem1 == 0)
                                            return;
                                        main.Gem1 = 255;
                                        Database.ModifyItem(main.Gem1, "Soc2", main.UniqueID);
                                        user.Send(Packet.ItemInfoPacket.Create(main, 3));
                                    }
                                    else
                                    {
                                        if (main.Gem2 == 0)
                                            return;
                                        main.Gem2 = 255;
                                        Database.ModifyItem(main.Gem2, "Soc2", main.UniqueID);
                                        user.Send(Packet.ItemInfoPacket.Create(main, 3));
                                    }
                                }
                                else
                                    Kernel.WriteLine("Unknown gem action type: " + subtype);
                                break;
                            }
                        #endregion
                        #region 1023: TeamAction
                        case 1023:
                            {
                                TeamAction receive = ptr;
                                Player target = Kernel.Clients[receive.Target];
                                if (target == null)
                                    return;
                                if (target == user && user.Team == null)//if I'm the sender and I have no team... make me a team.
                                {
                                    user.Team = new TeamClass();
                                    user.Team.Leader = user;
                                }
                                if (target == user || target.Team == null)
                                    user.Team.ProcessTeamPacket(user, receive);
                                else if (target.Team != null)
                                    target.Team.ProcessTeamPacket(user, receive);
                                break;
                            }
                        #endregion
                        #region 1024: AttributePoints
                        case 1024:
                            {
                                UInt32 str = BitConverter.ToUInt32(packet, 8);
                                UInt32 agi = BitConverter.ToUInt32(packet, 12);
                                UInt32 vit = BitConverter.ToUInt32(packet, 16);
                                UInt32 spi = BitConverter.ToUInt32(packet, 20);
                                UInt32 total = str + agi + vit + spi;

                                if (user.StatPoint >= total)
                                {
                                    user.StatPoint -= (UInt16)total;
                                    user.Strength += (UInt16)str;
                                    user.Agility += (UInt16)agi;
                                    user.Vitality += (UInt16)vit;
                                    user.Spirit += (UInt16)spi;

                                    user.Send(packet);
                                }
                                break;
                            }
                        #endregion
                        #region 1038: Stabilization
                        case 1038:
                            uint mode = BitConverter.ToUInt32(packet, 4);

                            uint itemID = BitConverter.ToUInt32(packet, 8); //Item you're trying to perm
                            uint purificationItemCount = BitConverter.ToUInt32(packet, 12); //The number of stones player added in
                            List<uint> smallStones = new List<uint>();
                            List<uint> bigStones = new List<uint>();
                            uint sum = 0;

                            if(!user.Inventory.ContainsKey(itemID)) //Make sure they have the item they're trying to perm
                            { return;}

                            for (int i = 0; i < purificationItemCount;  i++) //Read in the UIDs for each perm stone they're using
                            {
                                if (!user.Inventory.ContainsKey(BitConverter.ToUInt32(packet, 16 + i*4))) //Make sure they actually have those perm stones
                                {return;}

                                Structures.ItemInfo stone = user.Inventory[BitConverter.ToUInt32(packet, 16 + i*4)];//Get the item info

                                if(stone.StaticID == 723694) //Is it a small stone?
                                {
                                    smallStones.Add(BitConverter.ToUInt32(packet, 16 + i*4)); //Adds to smallstone list
                                    sum += 10;
                                }
                                if(stone.StaticID == 723695) //Or is it a big stone?
                                {
                                    bigStones.Add(BitConverter.ToUInt32(packet, 16 + i*4)); //Adds to largestone list
                                    sum += 100;
                                }

                                Kernel.WriteLine("{0}: {1} {2}", i, BitConverter.ToUInt32(packet, 16 + i*4), stone.StaticID);
                            }

                            switch(mode)
                            {
                                case 0:
                                    if (user.Inventory.ContainsKey(itemID))
                                    {
                                        Structures.ItemInfo item = user.Inventory[itemID];
                                        var level = ServerDatabase.Context.ItemRefineries.GetRefineryByItemId(itemID).Level;
                                        uint points = 0;
                                        switch (level)//Finds out how many points we need
                                        {
                                            case 0:
                                                break;
                                            case 1:
                                                points = 10;
                                                break;
                                            case 2:
                                                points = 30;
                                                break;
                                            case 3:
                                                points = 70;
                                                break;
                                            case 4:
                                                points = 150;
                                                break;
                                            case 5:
                                                points = 270;
                                                break;
                                        }

                                        Kernel.WriteLine("{0}: {1}", item.StaticID, level.ToString()); //Show item and RefineryLevel

                                        List<uint> used = new List<uint>();

                                        Kernel.WriteLine("{0}: {1}", sum, points);

                                        if (sum >= points) //If we have enough points
                                        {
                                            uint remainder = points;
                                            Kernel.WriteLine("{0}", remainder);
                                            while (remainder > 0)
                                            {
                                                if (remainder >= 100 && bigStones.Any()) //If we still can use 100 points and have a big stone [prioritize big stones]
                                                {
                                                    used.Add(bigStones[bigStones.Count - 1]); //Add it to the list of used stones
                                                    bigStones.RemoveAt(bigStones.Count - 1);//Remove it from the big stone list
                                                    remainder = remainder - 100;
                                                }
                                                else if (smallStones.Any()) //If there are no big stones left in the list, use small ones
                                                {
                                                    used.Add(smallStones[smallStones.Count - 1]); //Add it to the list of used stones
                                                    smallStones.RemoveAt(smallStones.Count - 1); // Remove from small stone list
                                                    remainder = remainder - 10;
                                                }
                                                if(!smallStones.Any() && bigStones.Any())
                                                {
                                                    used.Add(bigStones[bigStones.Count - 1]);
                                                    bigStones.RemoveAt(bigStones.Count - 1);
                                                    remainder = 0;
                                                }
                                                //Kernel.WriteLine("{0}", remainder);
                                            }

                                        }

                                        foreach (uint stone in used) //Remove stones used by itemid
                                        {
                                            Structures.ItemInfo delete = user.Inventory[stone];
                                            user.RemoveItem(delete);
                                        }

                                        var refinery_type = item.RefineryData.Type;

                                        if (item.RefineryData != null)//Then delete the old data
                                        {
                                            item.RefineryData.Delete();
                                        }

                                        item.RefineryData = new Structures.ItemRefinery();
                                        item.RefineryData.MaxRefinery(item.UniqueID, refinery_type);
                                        item.RefineryData.Type = refinery_type;
                                        item.RefineryData.SaveInfo();
                                        user.Send(RefineryInfoPacket.Create(item));
                                        user.Recalculate();
                                    }
                                    break;
                                case 1:
                                    if (user.Inventory.ContainsKey(itemID))
                                    {
                                        Structures.ItemInfo item = user.Inventory[itemID];
                                        var level = ServerDatabase.Context.ItemRefineries.GetArtifactByItemId(itemID).Level; //Gets the refinery level of the item
                                        uint points = 0;
                                        switch (level)//Finds out how many points we need
                                        {
                                            case 0:
                                                break;
                                            case 1:
                                                points = 10;
                                                break;
                                            case 2:
                                                points = 30;
                                                break;
                                            case 3:
                                                points = 60;
                                                break;
                                            case 4:
                                                points = 100;
                                                break;
                                            case 5:
                                                points = 150;
                                                break;
                                            case 6:
                                                points = 200;
                                                break;
                                        }

                                        Kernel.WriteLine("{0}: {1}", item.StaticID, level.ToString()); //Show item and RefineryLevel

                                        List<uint> used = new List<uint>();

                                        Kernel.WriteLine("{0}: {1}", sum, points);

                                        if (sum >= points) //If we have enough points
                                        {
                                            uint remainder = points;
                                            Kernel.WriteLine("{0}", remainder);
                                            while (remainder > 0)
                                            {
                                                if (remainder >= 100 && bigStones.Any()) //If we still can use 100 points and have a big stone [prioritize big stones]
                                                {
                                                    used.Add(bigStones[bigStones.Count - 1]); //Add it to the list of used stones
                                                    bigStones.RemoveAt(bigStones.Count - 1);//Remove it from the big stone list
                                                    remainder = remainder - 100;
                                                }
                                                else if (smallStones.Any()) //If there are no big stones left in the list, use small ones
                                                {
                                                    used.Add(smallStones[smallStones.Count - 1]); //Add it to the list of used stones
                                                    smallStones.RemoveAt(smallStones.Count - 1); // Remove from small stone list
                                                    remainder = remainder - 10;
                                                }

                                                if (!smallStones.Any() && bigStones.Any())
                                                {
                                                    used.Add(bigStones[bigStones.Count - 1]);
                                                    bigStones.RemoveAt(bigStones.Count - 1);
                                                    remainder = 0;
                                                }
                                            }

                                        }

                                        foreach (uint stone in used) //Remove stones used by itemid
                                        {
                                            Structures.ItemInfo delete = user.Inventory[stone];
                                            user.RemoveItem(delete);
                                        }
                                        var artifact_type = item.ArtifactData.Type;
                                        if (item.ArtifactData != null)
                                        {
                                            item.ArtifactData.Delete();
                                        }
                                        item.ArtifactData = new Structures.ItemRefinery();
                                        item.ArtifactData.MaxArtifact(item.UniqueID, artifact_type);
                                        item.ArtifactData.Action = RefineryInfoAction.Unknown8;
                                        item.ArtifactData.SaveInfo();
                                        user.Send(RefineryInfoPacket.Create(item));
                                        user.Recalculate();
                                    }
                                    break;
                            }



                            break;
                        #endregion
                        #region 1040: View Equipment
                        case 1040:
                            {
                                uint targ = BitConverter.ToUInt32(packet, 4);
                                if (targ > 0 && targ != user.UID)
                                {
                                    if (Kernel.Clients.ContainsKey(targ))
                                        Kernel.Clients[targ].Send(StatWindow.Create(Kernel.Clients[targ]));
                                }
                                else
                                    user.Send(StatWindow.Create(user));
                                break;
                            }
                        #endregion
                        #region 1052: Login request
                        case 1052://login request
                            {
                                // right now the player uid/login key well be passed reather than the whatever information that has been sent
                                // the key is the login key | idk about the mesage though you are gonna have to change it or whatever 
                                // the BitConverters needs to be change to whatever is the packet reader 

                                //About our packet reading. Take a look at how our packets are structured. We use implicit operators so you can treat data types as though they are the same. 
                                //In this example we treat the packet pointer as being a structure for the login packet. This populates the variables inside the login packet and lets us use them.
                                //ConnectPacket msg = ptr;
                                //In this case... msg.AccountId == your key variable and msg.Data is the UID variable.

                                //Good work though! I'll just make the couple minor changes

                                //uint key = (uint)BitConverter.ToUInt32(packet, 4); uint UID = (uint)BitConverter.ToUInt32(packet, 8);

                                //We will use this so it can try to pull the hidden user ID from database. This means players cannot simply send an auth response using someone's account ID and log in their character
                                ConnectPacket msg = ptr;
                                user.UID = Database.pullKey(user, msg.Data);

                                if (Kernel.Clients.ContainsKey(user.UID)) { Kernel.Clients[user.UID].Disconnect(true); user.Client.Disconnect(false); return; }
                                if (user.UID == 0 || user.Permission == PlayerPermission.Banned)
                                {
                                    user.Disconnect(false);
                                    return;
                                }

                                user.Send(Unknown2079Packet.Create(0));
                                user.Send(Unknown2078Packet.Create(0x4e591dba));

                                // if char loaded than everything is ok 
                                if (Database.PullLogin(user))
                                {
                                    if (Kernel.Clients.ContainsKey(user.UID))
                                        Kernel.Clients.Remove(user.UID);
                                    Kernel.Clients.Add(user.UID, user);
                                    Database.UpdateOnline();
                                    var response = new TalkPacket("SYSTEM", "ALLUSERS", "ANSWER_OK", string.Empty, 0xffffff, ChatType.Entrance);
                                    user.Send(response);
                                    if (user.Health < 1) user.Health = 1;
                                    user.Send(PlayerInfoPacket.Create(user));
                                    DataPacket mapEnter = new DataPacket(DataAction.EnterMap)
                                    {
                                        Id = user.MapID,
                                        Data1 = user.MapID,
                                        Data3Low = user.X,
                                        Data3High = user.Y
                                    };
                                    // user.Send(mapEnter); //idk why this is blocked 
                                    user.Send(DateTimePacket.Create());
                                    UserManager.LoginUser(user);
                                    user.GuildAttribute.Create();
                                    Database.ModifyAccount(0, "Hash", user.UID); Database.ModifyAccount(0, "Timestamp", user.UID);
                                }
                                // else if its a new player let him in right away 
                                else if (New_Role.Contains(user.UID))
                                {
                                    New_Role.Remove(user.UID);
                                    if (Kernel.Clients.ContainsKey(user.UID))
                                        Kernel.Clients.Remove(user.UID);
                                    Kernel.Clients.Add(user.UID, user);
                                    var response = new TalkPacket("SYSTEM", "ALLUSERS", "ANSWER_OK", string.Empty, 0xffffff, ChatType.Entrance);
                                    user.Send(response);
                                    Database.UpdateOnline();
                                    if (user.Health < 1) user.Health = 1;
                                    user.Send(PlayerInfoPacket.Create(user));
                                    DataPacket mapEnter = new DataPacket(DataAction.EnterMap)
                                    {
                                        Id = user.MapID,
                                        Data1 = user.MapID,
                                        Data3Low = user.X,
                                        Data3High = user.Y
                                    };
                                   //  user.Send(mapEnter);//idk why this is blocked 
                                    user.Send(DateTimePacket.Create());
                                    UserManager.LoginUser(user);
                                    user.GuildAttribute.Create();
                                    Database.ModifyAccount(0, "Hash", user.UID); Database.ModifyAccount(0, "Timestamp", user.UID);
                                }
                                // else if char was not loaded and its a new char creation send him the answer ok packet
                                else
                                {
                                    var response = new TalkPacket("SYSTEM", "ALLUSERS", "NEW_ROLE", string.Empty, 0xffffff, ChatType.Entrance);
                                    user.Send(response);
                                }
                                break;
                            }
                        //case 1052://login request
                        //    {
                        //        ConnectPacket msg = ptr;
                        //        var hash = msg.AccountId;
                        //        uint uid = Database.pullKey(user, hash);
                        //        user.UID = uid;
                        //        if (Kernel.Clients.ContainsKey(user.UID))
                        //        { Kernel.Clients[user.UID].Disconnect(true); user.Client.Disconnect(false); return; }//dc if same uid is logged in! (Checked both login and game server... Do NOT want 2 chars logged!!!)
                        //        if (user.UID == 0 || user.Permission == PlayerPermission.Banned)
                        //        {
                        //            user.Disconnect(false);
                        //            return;
                        //        }
                        //        var reply = "NEW_ROLE";
                        //        if (Database.PullLogin(user))//this is where it hits a problem.
                        //        {
                        //            reply = "ANSWER_OK";
                        //        }
                        //        var response = new TalkPacket("SYSTEM", "ALLUSERS", reply, string.Empty, 0xffffff, ChatType.Entrance);
                        //        user.Send(response);
                        //        if (reply == "ANSWER_OK")
                        //        {
                        //            if (Kernel.Clients.ContainsKey(user.UID))
                        //                Kernel.Clients.Remove(user.UID);
                        //            Kernel.Clients.Add(user.UID, user);
                        //            Database.UpdateOnline();
                        //            if (user.Health < 1) user.Health = 1;
                        //            user.Send(PlayerInfoPacket.Create(user));
                        //            DataPacket mapEnter = new DataPacket(DataAction.EnterMap)
                        //                                      {
                        //                                          Id = user.MapID,
                        //                                          Data1 = user.MapID,
                        //                                          Data3Low = user.X,
                        //                                          Data3High = user.Y
                        //                                      };
                        //            // user.Send(mapEnter);
                        //            user.Send(DateTimePacket.Create());
                        //            UserManager.LoginUser(user);
                        //            user.GuildAttribute.Create();
                        //        }
                        //        if (user.UID > 0)
                        //        { Database.ModifyAccount(0, "Hash", user.UID); Database.ModifyAccount(0, "Timestamp", user.UID); }
                        //        break;
                        //    }
                        #endregion
                        #region 1056: Trade request
                        case 1056:
                            {
                                TradePacket receive = ptr;
                                if (user.Trade == null)
                                {
                                    user.Trade = new Handler.TradeSequence
                                    {
                                        Owner = user
                                    };
                                }
                                user.Trade.ProcessTradePacket(user, receive);
                                //if (Kernel.Clients.ContainsKey(receive.Target))
                                //    Handler.Trade(receive, user, Kernel.Clients[receive.Target]);
                            }
                            break;

                        #endregion
                        #region 1058: Guild Donation Info

                        case 1058:
                            {
                                user.GuildAttribute.SendDonationInfoToClient();
                                break;
                            }

                        #endregion
                        #region 1101: GroundItem

                        case 1101:
                            {
                                Packet.GroundItem receive = ptr;
                                if (receive.Unknown1 == 0)
                                {
                                    if (!user.ContainsFlag1(Effect1.Vortex)) 
                                    user.AttackPacket = null;
                                    var groundItem = user.Map.Search<GroundItem>(receive.UID);
                                    if (groundItem != null)
                                    {
                                        if(!groundItem.CanLoot(user.UID))                                        
                                        {
                                            user.SendMessage("SYSTEM", user.Name, "This item is not yours. Please wait a while before picking it up.", 0xffffff, ChatType.System);
                                            return;
                                        }
                                        if (Calculations.Movement.Distance(user.X, user.Y, groundItem.X, groundItem.Y) > 1)
                                            return;
                                        if (user.Inventory.Count > 39 && groundItem.MoneyValue < 1)
                                        {
                                            user.SendMessage("SYSTEM", user.Name, "Your inventory is full", 0xffffff, ChatType.System);
                                            return;
                                        }
                                        receive.DropType = 3;
                                        user.SendToScreen(receive, true);
                                        receive.DropType = 2;
                                        user.SendToScreen(receive, true);
                                        if (groundItem.MoneyValue > 0)
                                        {
                                            user.SendMessage("SYSTEM", user.Name, "Looted " + groundItem.MoneyValue + " silvers", uint.MaxValue, ChatType.System);
                                            if (groundItem.IsCP)
                                                user.CP += groundItem.MoneyValue;
                                            else
                                                user.Money += groundItem.MoneyValue;
                                        }
                                        else
                                        {
                                            user.SendMessage("SYSTEM", user.Name, "You received a(n) " + groundItem.Information.ItemtypeData.Value.Name + " ", uint.MaxValue, ChatType.System);
                                            user.AddItem(groundItem.Information);
                                        }
                                        user.Map.Remove(groundItem, false);
                                    }
                                }
                                break;
                            }
                        #endregion
                        #region 1102: Warehouse

                        case 1102:
                            {
                                Packet.Items.Warehouse pack = ptr;
                                switch (pack.Action)
                                {
                                    case 0://view
                                        user.Warehouses.Show(pack.WarehouseID);
                                        break;
                                    case 1://insert
                                        if (!user.Inventory.ContainsKey(pack.Identifier))
                                            return;
                                        var itm = user.Inventory[pack.Identifier];
                                        user.Warehouses.Add(itm, pack.WarehouseID);
                                        break;
                                    case 2://remove
                                        pack.WhTYPE = 10;
                                        pack.Items = new List<Structures.ItemInfo>();
                                        user.Send(pack);
                                        user.Warehouses.Remove(pack.Identifier, pack.WarehouseID);
                                        break;
                                }
                                break;
                            }

                        #endregion
                        #region 1107: Guild

                        case 1107:
                            {
                                GuildPacket received = ptr;
                                switch (received.Action)
                                {
                                    case GuildAction.ApplyJoin:
                                        {
                                            if (received.Data == 0 || received.Data == user.UID)
                                                return;

                                            var target = UserManager.GetUser(received.Data);
                                            if (target == null) return;

                                            var guildId = user.GuildId;
                                            var targetGuildId = target.GuildId;
                                            var targetGuildRank = target.GuildRank;

                                            if (guildId != 0 || targetGuildId == 0 || targetGuildRank < GuildRank.HonorarySupervisor)
                                            {
                                                user.SendSystemMsg("Error: failed to join!");
                                                return;
                                            }

                                            var targetGuild = GuildManager.GetGuild(targetGuildId);
                                            if (targetGuild == null) return;

                                            if (!targetGuild.CanJoin(user))
                                            {
                                                user.SendSystemMsg("You are unable to join!");
                                                return;
                                            }

                                            if (!target.FetchApply(ApplyType.InviteJoinGuild, user.UID))
                                            {
                                                user.SetApply(ApplyType.JoinGuild, target.UID);
                                                target.Send(GuildPacket.Create(GuildAction.ApplyJoin, user.UID, 0));
                                                return;
                                            }

                                            user.GuildAttribute.JoinGuild(targetGuildId);

                                            break;
                                        }
                                    case GuildAction.InviteJoin:
                                        {
                                            if (received.Data == 0 || received.Data == user.UID)
                                                return;

                                            var target = UserManager.GetUser(received.Data);
                                            if (target == null) return;

                                            var guildId = user.GuildId;
                                            var guildRank = user.GuildRank;
                                            var targetGuildId = target.GuildId;
                                            var targetGuildRank = target.GuildRank;

                                            if (guildId == 0 || guildRank < GuildRank.HonorarySupervisor || targetGuildId != 0)
                                            {
                                                user.SendSystemMsg("Error: failed to join!");
                                                return;
                                            }

                                            var guild = GuildManager.GetGuild(guildId);
                                            if (guild == null) return;

                                            if (!target.FetchApply(ApplyType.JoinGuild, user.UID))
                                            {
                                                user.SetApply(ApplyType.InviteJoinGuild, target.UID);
                                                target.Send(GuildPacket.Create(GuildAction.InviteJoin, user.UID, 0));
                                                return;
                                            }

                                            target.GuildAttribute.JoinGuild(guildId);

                                            break;
                                        }
                                    case GuildAction.LeaveSyndicate:
                                        {
                                            var guildId = user.GuildId;
                                            if (guildId == 0) return;

                                            var rank = user.GuildRank;
                                            if (rank == GuildRank.GuildLeader)
                                            {
                                                user.SendSystemMsg("The guild is too big to be disbanded. Please transfer your guild power.");
                                                return;
                                            }

                                            user.GuildAttribute.LeaveGuild();

                                            break;
                                        }
                                    case GuildAction.QuerySyndicateName:
                                        {
                                            var guildId = received.Data;
                                            var guild = GuildManager.GetGuild(guildId);
                                            if (guild == null) return;

                                            var masterGuild = guild.MasterGuild;
                                            if (masterGuild == null) return;

                                            var msg = StringPacket.Create(StringAction.Guild, masterGuild.StringInfo, guildId);
                                            if (guild.Id != masterGuild.Id)
                                            {
                                                msg.Strings.AddString(guild.Name);
                                            }
                                            user.Send(msg);
                                            break;
                                        }
                                    case GuildAction.SetAlly:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            user.GuildAttribute.AddAlly(name);
                                            break;
                                        }
                                    case GuildAction.ClearAlly:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            var guildId = user.GuildId;
                                            var guild = GuildManager.GetGuild(guildId);
                                            if (guild == null) return;

                                            if (user.GuildRank != GuildRank.GuildLeader)
                                                return;

                                            var targetGuild = GuildManager.GetGuildByName(name);
                                            if (targetGuild == null) return;
                                            targetGuild = targetGuild.MasterGuild;

                                            for (var i = 0; i < 5; i++)
                                            {
                                                var targetId = guild.GetAlly(i);
                                                if (targetId == targetGuild.Id)
                                                {
                                                    guild.SetAlly(i, 0);
                                                    guild.BroadcastGuildMsg(GuildPacket.Create(GuildAction.ClearAlly, targetId, 0));

                                                    guild.BroadcastGuildMsg(string.Format("[rank 1000] {0} has removed Guild {1} from the allies list!", user.Name, targetGuild.Name));
                                                }
                                            }

                                            for (var i = 0; i < 5; i++)
                                            {
                                                var targetId = targetGuild.GetAlly(i);
                                                if (targetId == guild.Id)
                                                {
                                                    targetGuild.SetAlly(i, 0);
                                                    targetGuild.BroadcastGuildMsg(GuildPacket.Create(GuildAction.ClearAlly, targetId, 0));

                                                    targetGuild.BroadcastGuildMsg(string.Format("[rank 1000] {0} has removed Guild {1} from the allies list!", user.Name, targetGuild.Name));
                                                }
                                            }

                                            guild.SynchroInfo();
                                            targetGuild.SynchroInfo();

                                            break;
                                        }
                                    case GuildAction.SetEnemy:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            user.GuildAttribute.AddEnemy(name);
                                            break;
                                        }
                                    case GuildAction.ClearEnemy:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            user.GuildAttribute.RemoveEnemy(name);
                                            break;
                                        }
                                    case GuildAction.DonateMoney:
                                        {
                                            if (user.GuildId == 0) return;

                                            user.GuildAttribute.DonateMoney(received.Data);
                                            break;
                                        }
                                    case GuildAction.QuerySyndicateAttribute:
                                        {
                                            user.GuildAttribute.SendInfoToClient();
                                            break;
                                        }
                                    case GuildAction.DonateEMoney:
                                        {
                                            if (user.GuildId == 0) return;

                                            user.GuildAttribute.DonateEMoney(received.Data);
                                            break;
                                        }
                                    case GuildAction.Unknown23:
                                        {
                                            if (user.GuildId == 0) return;
                                            if (received.Data == 0 || received.Data == user.GuildId) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            var targetGuild = GuildManager.GetGuildByName(name);
                                            if (targetGuild == null) return;

                                            user.GuildAttribute.AddAlly(name);
                                            break;
                                        }
                                    case GuildAction.SetRequirement:
                                        {
                                            if (user.GuildId == 0) return;

                                            var guild = GuildManager.GetGuild(user.GuildId);
                                            if (guild == null) return;

                                            if (user.GuildRank != GuildRank.GuildLeader)
                                            {
                                                user.SendSystemMsg("You have not been authorized!");
                                                return;
                                            }

                                            guild.SetRequirements(received.RequiredLevel, received.RequiredMetempsychosis, received.RequiredProfession);
                                            break;
                                        }
                                    case GuildAction.SetAnnounce:
                                        {
                                            if (user.GuildId == 0) return;

                                            var guild = GuildManager.GetGuild(user.GuildId);
                                            if (guild == null) return;

                                            if (user.GuildRank != GuildRank.GuildLeader)
                                            {
                                                user.SendSystemMsg("You have not been authorized!");
                                                return;
                                            }

                                            string announce;
                                            if (!received.Strings.GetString(0, out announce)) return;

                                            guild.SetAnnounce(announce);
                                            break;
                                        }
                                    case GuildAction.PromoteMember:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            user.GuildAttribute.PromoteMember(name, GuildRank.DeputyLeader);
                                            break;
                                        }
                                    case GuildAction.DischargeMember:
                                        {
                                            if (user.GuildId == 0) return;

                                            string name;
                                            if (!received.Strings.GetString(0, out name)) return;

                                            user.GuildAttribute.DemoteMember(name);
                                            break;
                                        }
                                    case GuildAction.PromoteInfo:
                                        {
                                            if (user.GuildId == 0) return;

                                            user.GuildAttribute.SendPromotionInfoToClient();
                                            break;
                                        }
                                }
                                break;
                            }

                        #endregion
                        #region 1130: Title

                        case 1130:
                            {
                                TitlePacket receive = ptr;
                                switch (receive.Action)
                                {
                                    case TitleAction.SelectTitle:
                                        {
                                            user.SelectTitle(receive.Title);
                                            break;
                                        }
                                    case TitleAction.QueryTitle:
                                        {
                                            user.SendAllTitles();
                                            break;
                                        }
                                }
                                break;
                            }

                        #endregion
                        #region 1134: Quest
                        case 1134:
                            {
                                QuestPacket receive = ptr;
                                //Console.WriteLine("QuestPacket from {0}: {1} {2}", user.Character.Name, receive.Action, receive.Amount);
                                switch (receive.Action)
                                {
                                    case QuestAction.Begin:
                                        {
                                            var data = receive.GetData(0);
                                            Console.WriteLine("QuestPacket {0} began quest[{1},{2},{3}]",
                                                             user.Name, data.MissionId, data.Unknown2, data.Unknown1);
                                            break;
                                        }
                                    case QuestAction.List:
                                        {
                                            for (var i = 0; i < receive.Amount; i++)
                                            {
                                                var data = receive.GetData(i);
                                                //   Console.WriteLine("QuestPacket({0}) [{1},{2},{3}]", receive.Action,
                                                //                     data.MissionId, data.Unknown1, data.Unknown2);

                                                data.Unknown1 = 2;

                                            }
                                            user.Send(receive);
                                            break;
                                        }
                                    default:
                                        {
                                            // Console.WriteLine("Unhandled QuestPacket action: {0} {1}", receive.Action, receive.Amount);
                                            break;
                                        }
                                }
                                break;
                            }

                        #endregion
                        #region 1135: Quest
                        case 1135:
                            { break; }
                        #endregion
                        #region 2031:2032: Npc Usage
                        case 2031:
                            {
                                RequestNpc npc = ptr;
                                user.NpcInputBox = "";
                                if (packet[10] != 255)
                                    Handler.Npc(user, npc.NpcID, packet[10]);
                                if (user.Permission > PlayerPermission.Helper) 
                                user.SendMessage("SYSTEM", user.Name, "using NPC ID: " + npc.NpcID, 0xffffff, ChatType.Center);
                                break;
                            }

                        case 2032:
                            {
                                NpcReply npc = ptr;
                                switch (npc.Action)
                                {
                                    case DialogAction.Popup:
                                        {
                                            if (npc.TaskIndex != 255)
                                            {
                                                user.NpcInputBox = npc.Strings.GetString(0);
                                                Handler.Npc(user, user.LastNPC, npc.TaskIndex);
                                            }
                                            break;
                                        }
                                    case DialogAction.Answer:
                                        {
                                            if (npc.TaskIndex != 255)
                                            {
                                                user.NpcInputBox = npc.Strings.GetString(0);
                                                Handler.Npc(user, user.LastNPC, npc.TaskIndex);
                                            }
                                            break;
                                        }
                                    case DialogAction.TaskId:
                                        {
                                            switch (npc.TaskId)
                                            {
                                                case 31100: // kick guild member
                                                    {
                                                        string name;
                                                        if (!npc.Strings.GetString(0, out name)) return;

                                                        var guildId = user.GuildId;
                                                        var guildRank = user.GuildRank;
                                                        if (guildId == 0 || guildRank < GuildRank.GuildLeader)
                                                            return;
                                                        if (name == user.Name)
                                                            return;

                                                        var guild = GuildManager.GetGuild(guildId);
                                                        var target = UserManager.GetUser(name);
                                                        // NOTE: if (target == null) return false;

                                                        //if (!guild.ApplyKickoutMember(user, target))
                                                        //    return false;

                                                        var msg = string.Format("{0} did not abide by the rules of the guild and was driven out of the guild.", name);
                                                        guild.BroadcastGuildMsg(msg);

                                                        if (target != null)
                                                        {
                                                            target.SendSystemMsg("You have been dispelled from the guild by {0}.", user.Name);
                                                            if (target.GuildId == guildId)
                                                            {
                                                                target.GuildAttribute.LeaveGuild();
                                                                return;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            user.GuildAttribute.KickoutMember(name);
                                                            return;
                                                        }

                                                        //GuildPacket msgUnknown;
                                                        //if (GuildPacket.Create(GuildAction.Unknown44, 0, 0, out msgUnknown))
                                                        //    user.Send(msgUnknown);

                                                        break;
                                                    }
                                                default:
                                                    {
                                                        Console.WriteLine("Unprocessed client task: {0}", npc.TaskId);
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        #endregion
                        #region 2036 Composition
                        case 2036:
                            {
                                Handler.CompositionHandler(packet, user);
                                break;
                            }
                        #endregion
                        #region 2048 Lock

                        case 2048:
                            {
                                uint id = BitConverter.ToUInt32(packet, 4);
                                if (user.Inventory.ContainsKey(id))
                                {
                                    Structures.ItemInfo item = user.Inventory[id];
                                    item.Locked = !item.Locked;
                                    user.Send(Packet.ItemInfoPacket.Create(item, 3));
                                    if (item.Locked)
                                        Database.ModifyItem(1, "Locked", item.UniqueID);
                                    else
                                        Database.ModifyItem(0, "Locked", item.UniqueID);
                                }
                            }
                            break;
                        #endregion
                        #region 2050 Broadcast
                        case 2050:
                            {                            
                                Packet.Broadcast received = ptr;
                                switch (received.Subtype)
                                {
                                    case 259://3
                                        if (user.Level < 50)
                                            return;
                                        if (user.CP < 5)
                                            return;
                                        Packet.TalkPacket tosend = new TalkPacket(received.StringPacker.GetString(0));
                                        tosend.Type = ChatType.BroadcastMessage;
                                        tosend.Speaker = user.Name;
                                        user.CP -= 5;
                                        Kernel.Broadcasts.Add(tosend);
                                        break;
                                    default:
                                        Console.WriteLine("Unknown broadcast Type: " + received.Subtype);
                                        break;
                                }
                                break;
                            } 
                        #endregion
                        #region 2064: Nobility
                        case 2064://nobility
                            {
                                HandleNobility(ptr);
                                break;
                            }
                        #endregion
                        #region 2076: Apply Refinery
                        case 2076:
                            {
                                switch (BitConverter.ToUInt32(packet, 4))
                                {
                                    #region Refinery Item
                                    case 0:
                                        {
                                            uint mainuid = BitConverter.ToUInt32(packet, 8);
                                            uint minoruid = BitConverter.ToUInt32(packet, 12);
                                            Structures.ItemInfo main = user.Equipment.GetItemByUID(mainuid);
                                            if (main == null)
                                                return;
                                            if (!user.Inventory.ContainsKey(minoruid))
                                                return;
                                            Structures.ItemInfo minor = user.Inventory[minoruid];

                                            //check subtype!

                                            var mainType = Calculations.Items.TypeOfItem(main.StaticID);
                                            var refineryType = ServerDatabase.Context.Refinerytypes.GetByIdCached(minor.StaticID).ItemSubtype;

                                            bool canUse = false;
                                            switch (refineryType)
                                            {
                                                case 110://head
                                                    canUse = ((main.StaticID >= 111003 && main.StaticID <= 118309) || (main.StaticID >= 123000 && main.StaticID <= 123309) || (main.StaticID >= 141003 && main.StaticID <= 143309));
                                                    break;
                                                case 120://neck
                                                    canUse = (main.StaticID >= 120001 && main.StaticID <= 120269);
                                                    break;
                                                case 121://bag
                                                    canUse = (main.StaticID >= 121003 && main.StaticID <= 121269);
                                                    break;
                                                case 139://armor
                                                    canUse = (main.StaticID >= 130003 && main.StaticID <= 136309);
                                                    break;
                                                case 150://ring
                                                    canUse = (main.StaticID >= 150000 && main.StaticID <= 150320);
                                                    break;
                                                case 151://heavyRing
                                                    canUse = (main.StaticID >= 151013 && main.StaticID <= 151269);
                                                    break;
                                                case 152://brace
                                                    canUse = (main.StaticID >= 152013 && main.StaticID <= 152279);
                                                    break;
                                                case 160://boot
                                                    canUse = (main.StaticID >= 160013 && main.StaticID <= 160249);
                                                    break;
                                                case 421://bs
                                                    canUse = (main.StaticID >= 421003 && main.StaticID <= 421439);
                                                    break;
                                                case 444://1h
                                                    canUse = ((main.StaticID >= 410003 && main.StaticID <= 420439) || (main.StaticID >= 422000 && main.StaticID <= 490439) || (main.StaticID >= 601000 && main.StaticID <= 610439));
                                                    break;
                                                case 500://bow
                                                    canUse = (main.StaticID >= 500003 && main.StaticID <= 500429);
                                                    break;
                                                case 555://2h
                                                    canUse = (main.StaticID >= 510003 && main.StaticID <= 580439);
                                                    break;
                                                case 900://shield
                                                    canUse = (main.StaticID >= 900000 && main.StaticID <= 900309);
                                                    break;
                                            }

                                            if (!canUse)
                                            {
                                                user.SendMessage("SYSTEM", user.Name, "Error! Cannot apply this type of refinery to a " + main.ItemtypeData.Value.Name, uint.MaxValue, ChatType.Center);
                                                return;
                                            }

                                            if (main.RefineryData != null)
                                            {
                                                main.RefineryData.Delete();
                                            }
                                            main.RefineryData = new Structures.ItemRefinery();
                                            main.RefineryData.CreateRefinery(main.UniqueID, minor.StaticID);

                                            main.RefineryData.SaveInfo();
                                            user.Send(RefineryInfoPacket.Create(main));
                                            user.RemoveItem(minor);
                                            user.RecalculateAttack();
                                            user.SendMessage("SYSTEM", user.Name, "Success! Refinery bonuses have been added to your " + main.ItemtypeData.Value.Name + ". Please check.", uint.MaxValue, ChatType.Center);

                                            break;
                                        }
                                    #endregion
                                    #region Artifact Item
                                    case 1:
                                        {
                                            uint mainuid = BitConverter.ToUInt32(packet, 8);
                                            uint minoruid = BitConverter.ToUInt32(packet, 12);
                                            if (!user.Inventory.ContainsKey(minoruid))
                                                return;
                                            if (!user.Inventory.ContainsKey(mainuid))
                                                return;
                                            Structures.ItemInfo main = user.Inventory[mainuid];
                                            Structures.ItemInfo minor = user.Inventory[minoruid];

                                            if (!user.HasItem(1088001, (int)minor.ItemtypeData.Value.DragonSoulReq))
                                                return;
                                            if (minor.ItemtypeData.Value.Requirements.Level > main.ItemtypeData.Value.Requirements.Level)
                                                return;
                                            user.RemoveItem(1088001, (int)minor.ItemtypeData.Value.DragonSoulReq);
                                            if (main.ArtifactData != null)
                                                main.ArtifactData.Delete();
                                            main.ArtifactData = new Structures.ItemRefinery();
                                            main.ArtifactData.CreateArtifact(main.UniqueID, minor.StaticID);
                                            main.ArtifactData.SaveInfo();
                                            user.RemoveItem(minor);
                                            user.Send(RefineryInfoPacket.Create(main));
                                            user.SendMessage("SYSTEM", user.Name, "Success! Artifact bonuses have been added to your " + main.ItemtypeData.Value.Name + ". Please check.", uint.MaxValue, ChatType.Center);
                                            user.Recalculate();
                                            break;
                                        }
                                    #endregion
                                }
                                break;
                            }
                        #endregion
                        #region Poker Shit
                        case 2090:
                            {
                                PokerScreen receive = ptr;
                                receive.characteruid = user.UID;
                                //receive.PlayerStatus = PlayerPokerStatus.Normal;
                                Console.WriteLine("Seat Testing ID: " + receive.SeatID);                                
                                user.Send(receive);
                                break;
                            }
                        case 2096:
                            {
                                Poker2096 receive = ptr;
                                switch (receive.Type)
                                {
                                    case 1://Player is leaving!                                  
                                        receive.Subtype = Kernel.Table.ID;//Table we are leaving
                                        receive.UID = user.UID;//who is leaving
                                        user.Send(receive);//WORKS to remove from main screen... we now just need something to remove from the sitting state... which would be... 2171?

                                        //This EXACT packet is being used to make players stand up from table... why the fuck won't it work?
                                        PokerAction tryRemove = new PokerAction
                                        {
                                            ActionType = PokerActionType.LeaveTable,
                                            TargetID = Kernel.Table.UID,
                                            Sender = user.UID,
                                            SeatID = user.PokerSeat
                                        };
                                        user.SendToScreen(tryRemove, true);
                                        //Remove us from players and update screen with how many are playing
                                        Kernel.Table.Users.Remove(user);
                                        user.SendToScreen(new Packet.DataPacket
                                        {
                                            Id = Kernel.Table.UID,
                                            Data1 = (uint)(Kernel.Table.Users.Count),
                                            Action = (DataAction)235
                                        }, true);

                                        //This can respawn table to us causing us to not be sitting... this is WRONG though
                                        //user.Send(SpawnTable.Create(Kernel.Table));
                                        break;
                                }
                                break;
                            }
                        #endregion
                        #region 2102: Guild Member List

                        case 2102:
                            {
                                GuildMemberPacket received = ptr;

                                var guildId = user.GuildId;
                                var guild = GuildManager.GetGuild(guildId);
                                if (guild == null) return;

                                guild.SendMemberList(user, received.StartIndex);
                                break;
                            }

                        #endregion
                        #region 2171: PokerAction
                        case 2171:
                            {
                                PokerAction receive = ptr;
                                //we need to pull table by table ID!
                                switch (receive.ActionType)
                                {
                                    case PokerActionType.JoinTable://join??
                                        Kernel.Table.Users.Add(user);//we add us to users
                                        //we send the updated player count to screen?
                                        user.SendToScreen(new Packet.DataPacket
                                        {
                                            Id = receive.TargetID,
                                            Data1 = (uint)(Kernel.Table.Users.Count),
                                            Action = (DataAction)235
                                        }, true);
                                        user.SendToScreen(receive, true);
                                        user.PokerSeat = receive.SeatID;

                                        //send us to members
                                        foreach (Player loop in Kernel.Table.Users)
                                            loop.Send(PokerScreen.Create(Kernel.Table, user));
                                        //send members to us
                                        foreach (Player loop in Kernel.Table.Users)
                                            user.Send(PokerScreen.Create(Kernel.Table, loop));
                                        break;
                                    case PokerActionType.Watch:
                                        Kernel.WriteLine(user.Name + " wants to watch table UID: " + Kernel.Table.UID);
                                        break;
                                    default:
                                        Console.WriteLine("Unknown poker action type from player: " + user.Name);
                                        break;
                                }
                                Kernel.Table.HandleAction(receive, user);//Watch or join                                             
                                break;
                            }
                        #endregion
                        #region 2205 Arena Action
                        case 2205:
                            {
                                ArenaAction receive = packet;
                                switch (receive.DialogID)
                                {
                                    case 0://signup
                                        {
                                            ArenaQualifier.DoSignup(user);
                                            Kernel.WriteLine(user.Name + " joining arena");
                                            user.Send(packet);
                                            break;
                                        }
                                    case 1://quit waiting
                                        {
                                            ArenaQualifier.DoQuit(user);
                                            user.Send(packet);
                                            break;
                                        }
                                    case 3:
                                        {
                                            switch (receive.OptionID)
                                            {
                                                case 1:

                                                    ArenaQualifier.DoAccept(user);
                                                    break;
                                                case 2:
                                                    ArenaQualifier.DoGiveUp(user);
                                                    break;
                                            }
                                            break;
                                        }
                                    case 4:
                                        {
                                            ArenaQualifier.DoQuit(user);
                                            break;
                                        }
                                    case 5:
                                        {
                                            Kernel.WriteLine("buying arena points");
                                            if (user.ArenaStats.ArenaPoints <= 1500)
                                            {
                                                if (user.Money >= 9000000)
                                                {
                                                    user.Money -= 9000000;
                                                    user.ArenaStats.ArenaPoints += 1500;
                                                    Database.ModifyCharacter(user.ArenaStats.ArenaPoints, "ArenaPoints", user.UID);
                                                    user.Send(user.ArenaStats);
                                                }
                                            }
                                            break;
                                        }
                                    case 10:
                                        {
                                            Kernel.WriteLine("win/lose dialog???");
                                            switch (receive.OptionID)
                                            {
                                                case 0:
                                                    {
                                                        ArenaQualifier.DoSignup(user);
                                                        Kernel.WriteLine("do signup???");
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        #endregion
                        #region 2206 RequestGroupList
                        case 2206:
                            {
                                user.Send(ArenaQualifier.BuildPacket(BitConverter.ToUInt16(packet, 4)));
                                break;
                            }
                        #endregion
                        #region 2207 Show Rankings Page
                        case 2207:
                            {
                                Kernel.WriteLine(user.Name + " requesting rankings page");
                                break;
                            }
                        #endregion
                        #region 2208 Arena Request Winners
                        case 2208:
                            user.Send(Packet.ArenaWiners.Create());
                            break;
                        #endregion
                        #region 2209 Request My ArenaStats
                        case 2209:
                            user.Send(user.ArenaStats);
                            break;
                        #endregion
                        #region Spectate Fight
                        /*
                    case 2211:
                        {
                            ushort Type = BitConverter.ToUInt16(packet, 4);
                            uint Fighter = BitConverter.ToUInt32(packet, 6);
                            if (Type == 0)
                            {
                                if (ServerBase.Kernel.GamePool.ContainsKey(Fighter))
                                {
                                    Client.GameState Client = ServerBase.Kernel.GamePool[Fighter];
                                    if (Client.QualifierGroup != null)
                                    {
                                        if (Client.QualifierGroup.Inside)
                                        {
                                            if (!Client.QualifierGroup.Done)
                                            {
                                                Client.QualifierGroup.BeginWatching(client);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Type == 1)
                            {
                                Game.ConquerStructures.Arena.QualifyEngine.DoLeave(client);
                            }
                            else if (Type == 4)
                            {
                                string name = "";
                                for (int c = 22; c < packet.Length; c++)
                                {
                                    if (packet[c] != 0)
                                        name += (char)packet[c];
                                    else
                                        break;
                                }
                                Game.ConquerStructures.Arena.QualifyEngine.DoCheer(client, name);
                            }
                            break;
                        }*/
                        #endregion
                        #region 10005: Walk
                        case 10005:
                            {
                                if (user.StatusSet.ContainsKey(StatusType.ReviveProtection))
                                    user.DetachStatus(StatusType.ReviveProtection);
                                user.LastWalk = Native.timeGetTime();
                                if(!user.ContainsFlag1(Effect1.Vortex))
                                    user.AttackPacket = null;
                                user.Action = ConquerAction.None;
                                WalkPacket receive = ptr;

                                var dir = 0;
                                int newX = 0, newY = 0;

                                switch (receive.Mode)
                                {
                                    case MoveMode.Walk:
                                    case MoveMode.Run:
                                        {
                                            dir = (int)receive.Direction % 8;
                                            newX = user.X + Common.DeltaX[dir];
                                            newY = user.Y + Common.DeltaY[dir];
                                            if (user.ContainsFlag1(Effect1.Riding))
                                            {
                                                user.Vigor -= 1;
                                            }
                                            break;
                                        }
                                    case MoveMode.Mount:
                                        {
                                            dir = (int)receive.Direction % 24;
                                            newX = user.X + Common.DeltaMountX[dir];
                                            newY = user.Y + Common.DeltaMountY[dir];
                                            if (user.ContainsFlag1(Effect1.Riding))
                                            {
                                                user.Vigor -= 2;
                                            }
                                            break;
                                        }
                                }

                                if (user.Map.Translate(user, new Point(newX, newY)))
                                {
                                    user.Direction = (byte)dir;
                                    user.SendToScreen(receive, true);
                                    user.UpdateBroadcastSet();
                                }
                                if (user.ContainsFlag2(Effect2.CaryingFlag) && Kernel.CaptureTheFlag != null && user.Map.ID == 2060)
                                {
                                    //check dist to own base...
                                    if (Calculations.Movement.Distance(user.X, user.Y, user.CtfTeam.Flag.X, user.CtfTeam.Flag.Y) < 8 && user.CtfTeam.FlagAtBase)
                                    {
                                        user.CtfTeam.Capture(user);
                                    }

                                }
                                break;
                            }
                        #endregion
                        #region 10010: General Data
                        case 10010:
                            {
                                DataPacket receive = ptr;
                                switch (receive.Action)
                                {
                                    #region DieQuestion
                                    case DataAction.DieQuestion:
                                        user.ToGhost();
                                        break;
                                    #endregion
                                    #region EndTransform
                                    case DataAction.AbortTransform:

                                        user.AttackPacket = null;
                                        user.Transformation = 0;
                                        break;

                                    #endregion
                                    #region EndFly
                                    case DataAction.EndFly:
                                        user.RemoveFlag1(Effect1.Fly);
                                        break;
                                    #endregion
                                    #region ViewGears
                                    case DataAction.QueryFriendEquip:
                                    case DataAction.QueryEquipment:
                                        {                                            
                                            if (!Kernel.Clients.ContainsKey(receive.Data1))
                                                return;
                                            Player target = Kernel.Clients[receive.Data1];
                                            user.Send(Packet.SpawnPlayerPacket.Create(target));
                                            for (byte i = 1; i <= (int)ItemLocation.Steed; i++)
                                            {
                                                var item = target.Equipment.GetItemBySlot((ItemLocation)i);
                                                if (item == null) continue;
                                                if (!item.ItemtypeData.HasValue) continue;
                                                user.Send(Packet.ViewItem.Create(target.UID, item));
                                                if (item.RefineryData != null || item.ArtifactData != null)
                                                    user.Send(Packet.RefineryInfoPacket.Create(item));
                                            }

                                            user.Send(StringPacket.Create(StringAction.QueryMate, target.Spouse));

                                            //StringPacket sp = new StringPacket();
                                            //sp.Strings = new NetStringPacker();
                                            //sp.UID = user.UID;
                                            //sp.Type = 16;
                                            //sp.Strings.AddString(target.Spouse);
                                            //user.Send(sp);
                                            //sp.Type = 10;
                                            //user.Send(sp);
                                            break;
                                        }

                                    #endregion
                                    #region Revive
                                    case DataAction.Revive:
                                        {
                                            user.AttachStatus(StatusType.ReviveProtection, 0, 5);
                                            user.AttackPacket = null;
                                            if (!user.Alive && DateTime.Now > user.KilledAt.AddSeconds(19))
                                            {
                                                user.Stamina = user.MaxStamina;
                                                user.Health = user.MaxHealth;
                                                user.RemoveFlag1(Effect1.Ghost);
                                                user.RemoveFlag1(Effect1.Dead);
                                                user.Transformation = 0;
                                                var mq = user.Map.QueryScreen(user);
                                                foreach (ILocatableObject o in mq)
                                                {
                                                    if (o is Monster && !Kernel.ActiveMonsters.Contains(o) && (o as Monster) != null)
                                                        Kernel.ActiveMonsters.Add(o as Monster);
                                                }
                                                Packet.UseSpell tosend = new Packet.UseSpell
                                                {
                                                    SpellID = 1050,
                                                    TargetX = user.X,
                                                    TargetY = user.Y,
                                                    AttackerID = user.UID,
                                                };
                                                tosend.AddTarget(user.UID, 0, true);
                                                if (user.Map.ID == 1038)
                                                { user.CanEnterGW = DateTime.Now.AddMinutes(5); user.ChangeMap(438, 379, 1002); }
                                                else if (user.Map.ID == 2060 && Kernel.CaptureTheFlag != null)
                                                {
                                                    if (Kernel.CaptureTheFlag.Red.Members.Contains(user))
                                                    {
                                                        Point rezPt = Kernel.CaptureTheFlag.RedSpawns[Kernel.RandomNext(0, 1)];
                                                        user.ChangeMap((ushort)rezPt.X, (ushort)rezPt.Y, 2060);
                                                    }
                                                    if (Kernel.CaptureTheFlag.Blue.Members.Contains(user))
                                                    {
                                                        Point rezPt = Kernel.CaptureTheFlag.BlueSpawns[Kernel.RandomNext(0, 1)];
                                                        user.ChangeMap((ushort)rezPt.X, (ushort)rezPt.Y, 2060);
                                                    }
                                                    if (Kernel.CaptureTheFlag.White != null)
                                                        if (Kernel.CaptureTheFlag.White.Members.Contains(user))
                                                        {
                                                            Point rezPt = Kernel.CaptureTheFlag.WhiteSpawns[Kernel.RandomNext(0, 1)];
                                                            user.ChangeMap((ushort)rezPt.X, (ushort)rezPt.Y, 2060);
                                                        }
                                                    if (Kernel.CaptureTheFlag.Black != null)
                                                        if (Kernel.CaptureTheFlag.Black.Members.Contains(user))
                                                        {
                                                            Point rezPt = Kernel.CaptureTheFlag.BlackSpawns[Kernel.RandomNext(0, 1)];
                                                            user.ChangeMap((ushort)rezPt.X, (ushort)rezPt.Y, 2060);
                                                        }
                                                }
                                                else if (Kernel.CQMAP.ContainsKey(user.Map.ID) && user.Map.DynamicID == user.Map.ID)
                                                {
                                                    var currentMap = Kernel.CQMAP[user.Map.ID];
                                                    if (currentMap.RebornMap != user.MapID && Kernel.CQMAP.ContainsKey(user.MapID))                                                    
                                                        currentMap = Kernel.CQMAP[currentMap.RebornMap];    
                                                    user.ChangeMap(currentMap.StartX, currentMap.StartY, currentMap.RebornMap);
                                                }
                                                else
                                                    Console.WriteLine("No map info loaded for map ID: " + user.Map.ID);
                                                user.SendToScreen(tosend, true);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Request Surroundings
                                    case DataAction.GetSurroundings:
                                        {
                                            if(user.FinishedLogin)
                                                user.UpdateBroadcastSet(true);
                                            if (user.Shop != null && user.Shop.Vending)
                                                user.Shop.StopVending();
                                            break;
                                        }

                                    #endregion
                                    #region Change Action
                                    case DataAction.ChangeAction:
                                        {
                                            if (user.Action != ConquerAction.Sit && (ConquerAction)receive.Data1Low == ConquerAction.Sit)
                                                user.SitAt = DateTime.Now;
                                            user.Action = (ConquerAction)receive.Data1Low;
                                            user.SendToScreen(receive, false);
                                            break;
                                        }
                                    #endregion
                                    #region Change Direction
                                    case DataAction.ChangeDirection:
                                        {
                                            user.Direction = (byte)receive.Direction;
                                            user.SendToScreen(receive, false);
                                            break;
                                        }
                                    #endregion
                                    #region PkMode
                                    case DataAction.SetPkMode: 
                                        if (!user.ContainsFlag1(Effect1.Vortex)) 
                                        user.AttackPacket = null;
                                        user.AttackMode = (PkMode)receive.Data1Low;
                                        user.Send(receive);
                                        break;
                                    #endregion
                                    #region Request Friends
                                    case DataAction.GetGoodFriend:
                                        {
                                            user.CreateAllFriend();
                                            user.SendFriendInfo();

                                            Database.PullProfs(user);
                                            Database.PullSkills(user);
                                            Database.LoadTitles(user);
                                            UpdatePacket update = UpdatePacket.Create(user.UID);

                                            if (user.LuckyTimeEnds > DateTime.Now)
                                                update.AddUpdate(UpdateType.LuckyTimeTimer, Database.MsFromNow(user.LuckyTimeEnds));
                                            if (user.BlessingEnds > DateTime.Now)
                                            {
                                                user.AddFlag1(Effect1.HeavenBless);
                                                update.AddUpdate(UpdateType.HeavensBlessing, Database.SecondsFromNow(user.BlessingEnds));
                                                update.AddUpdate(UpdateType.OnlineTraining, 0);
                                                user.NextOnlineTGExp = DateTime.Now.AddMinutes(10);
                                            }
                                            if (user.ExpBoostEnds > DateTime.Now)
                                                update.AddUpdate(UpdateType.DoubleExpTimer, Database.SecondsFromNow(user.ExpBoostEnds));
                                            update.AddUpdate(UpdateType.VIPLevel, 6);
                                            update.AddUpdate(UpdateType.Merchant, 255);
                                            update.AddUpdate(UpdateType.QuizPoints, 10000);
                                            update.AddUpdate(UpdateType.PkPt, user.PkPoints);
                                            user.Send(update);
                                            user.Send(receive);

                                            break;
                                        }

                                    #endregion
                                    #region Change Face
                                    case DataAction.ChangeFace:
                                        if (user.Money > 500)
                                        {
                                            user.Money -= 500;
                                            user.Face = receive.Data1Low;
                                            user.Send(receive);
                                        }
                                        break;
                                    #endregion
                                    #region Request Hotkeys

                                    case DataAction.GetItemSet:
                                        {
                                            user.Send(receive);
                                            if (!Kernel.Maps.ContainsKey(user.MapID))
                                                Kernel.Maps.Add(user.MapID, new Map(user.MapID, user.MapID));
                                            Kernel.Maps[user.MapID].Insert(user);
                                            // time to remove expired refineries/artifacts
                                            ServerDatabase.Context.ItemRefineries.DeleteUserRefineries(user.UID);

                                            Database.LoadGears(user);
                                            user.Equipment.DisplayGears();
                                            user.Send(new DataPacket(DataAction.SetPkMode) //set default mode to capture on login!
                                                          {
                                                              Data1Low = (ushort)PkMode.Capture,
                                                              Id = user.UID
                                                          });
                                            user.FinishedLogin = true;

                                            user.AttachStatus(StatusType.ReviveProtection, 0, 10);
                                            user.SendMOTD();
                                            user.Recalculate();
                                                user.LoadNobility();
                                                user.SendNobilityIconToScreen();
                                            Database.ModifyCharacter(1, "Online", user.UID);
                                            user.DelayedActions.AddAction(DelayedActionType.UpStam, user.UpStam, 600);
                                            user.DelayedActions.AddAction(DelayedActionType.UpXP, user.UpXP, 3000);
                                            user.DelayedActions.AddAction(DelayedActionType.PediodicSave, user.PeriodicSave, 60000);
                                            user.DelayedActions.AddAction(DelayedActionType.LowerPKPt, user.LowerPK, 300000);
                                            user.UpdateBroadcastSet(true);
                                            user.SendSystemMsg("All users gain 3x exp for the next day!");
                                            break;
                                        }

                                    #endregion
                                    #region WeaponSkillSet
                                    case DataAction.GetWeaponSkillSet:
                                        {
                                            user.Send(receive);
                                            break;
                                        }
                                    #endregion
                                    #region MagicSet
                                    case DataAction.GetMagicSet:
                                        {
                                            user.Send(receive);
                                            break;
                                        }
                                    #endregion
                                    #region Friend/Enemy/Guild Set
                                    case DataAction.GetSynAttr:
                                        {
                                            if (user == null) return;

                                            user.GuildAttribute.SendInfoToClient();
                                            var guild = GuildManager.GetGuild(user.GuildId);
                                            if (guild != null)
                                            {
                                                guild = guild.MasterGuild;
                                                guild.SendInfoToClient(user);
                                            }

                                            if (GuildWar.CurrentWinner == user.Guild)
                                                if (user.GuildRank == GuildRank.GuildLeader)
                                                    user.AddFlag1(Effect1.TopGuild);
                                                else if (user.GuildRank == GuildRank.DeputyLeader)
                                                    user.AddFlag1(Effect1.TopDep); 
                                           /* if (Database.CanVote(user))
                                            {
                                                user.LastNPC = 123;
                                                user.Send(NpcReply.Create(1, DialogAction.Popup, "This was already in the code and has no use...!"));
                                            }
                                            */
                                            //user.NobilityInformation = NobilityRankingInfo.Initialize(user);
                                            //if (NobilityRankings.NobilityRecords.ContainsKey(user.UID))
                                            //    user.NobilityInformation = NobilityRankings.NobilityRecords[user.UID];
                                            //user.Send(Packet.Nobility.CreateIcon(user));

                                            Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " has logged into the game"));
                                            break;
                                        }
                                    #endregion
                                    #region Portal
                                    case DataAction.ChangeMap://portal                                  
                                        //credits hybrid: Project Manifesto
                                        {
                                            if (!user.ContainsFlag1(Effect1.Vortex)) 
                                            user.AttackPacket = null;
                                            bool Failed = true;
                                            if (user.Map != null)
                                            {
                                                if (Calculations.Movement.Distance(user.X, user.Y, receive.Data1Low, receive.Data1High) <= 5)
                                                {
                                                    uint DestMapID;
                                                    ushort DestX, DestY;
                                                    if (Handlers.Portal.FindPortal((uint)user.Map.ID, receive.Data1Low, receive.Data1High, out DestMapID, out  DestX, out  DestY))
                                                    {
                                                        if (Kernel.DmapHandler.Maps.ContainsKey((int)DestMapID))
                                                        {
                                                            if (Kernel.DmapHandler.Maps[(int)DestMapID].Check(DestX, DestY))
                                                                user.ChangeMap(DestX, DestY, DestMapID);
                                                            else
                                                                Console.WriteLine("ERROR: INVALID DMAP FOR X: " + DestX + " Y: " + DestY);
                                                            Failed = false;
                                                        }
                                                    }
                                                }
                                            }
                                            if (Failed)
                                            {
                                                Console.WriteLine("Unknown portal Map: " + user.Map.ID + " Start X: " + receive.Data1Low + " Y: " + receive.Data1High);
                                                user.ChangeMap(400, 400, 1002);
                                            }
                                            //TODO Previous map/x/y

                                            break;
                                        }
                                    #endregion
                                    #region QueryPlayer
                                    case DataAction.QueryPlayer:
                                        Entity targ = user.Map.Search(receive.Data1) as Entity;
                                        if (targ != null)
                                        {
                                            if (targ is Player)
                                                user.Send(SpawnPlayerPacket.Create(targ as Player));
                                            else if (targ is Monster)
                                                user.Send(SpawnPlayerPacket.Create(targ as Monster));
                                        }
                                        break;
                                    #endregion
                                    #region EnterMap
                                    case DataAction.EnterMap:
                                        {
                                            //Handle Actual enter of game
                                            if(Kernel.CQMAP.ContainsKey(user.MapID))
                                            {
                                                var inf = Kernel.CQMAP[user.MapID];
                                                if (inf.ContainsFlag(MapTypeFlags.RecordDisable))//Current map won't let us log on it... Lets pull our previous map and start at its default coord!
                                                {
                                                    Console.WriteLine("Map ID: " + inf.MapID + " has record disable active");
                                                    if (Kernel.CQMAP.ContainsKey(user.PreviousMap))
                                                    {
                                                        var newinf = Kernel.CQMAP[user.PreviousMap];
                                                        user.MapID = newinf.MapID;
                                                        user.X = newinf.StartX;
                                                        user.Y = newinf.StartY;
                                                    }
                                                    else
                                                        Console.WriteLine("No map info for: " + user.Map.ID);
                                                }
                                            }
                                            else
                                                Console.WriteLine("No map info for: " + user.MapID);
                                            DataPacket mapEnter = new DataPacket(DataAction.EnterMap)
                                            {
                                                Id = user.MapID,
                                                Data1 = user.MapID,
                                                Data3Low = user.X,
                                                Data3High = user.Y
                                            };
                                            user.Send(mapEnter);
                                            if (Kernel.CQMAP.ContainsKey(user.MapID))
                                            {
                                                var cqMap = Kernel.CQMAP[user.MapID];
                                                user.Send(Packet.MapInfoPacket.Create(cqMap.MapID, cqMap.DocID, cqMap.Type));
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region DeleteCharacter
                                    case DataAction.DelRole:
                                        Database.DeleteCharacter(user);
                                        user.Disconnect(false);
                                        break;
                                    #endregion
                                    #region Jump
                                    case DataAction.Jump:
                                        {
                                            if (user.StatusSet.ContainsKey(StatusType.ReviveProtection))
                                                user.DetachStatus(StatusType.ReviveProtection);
                                            if (user.LastClientJump >= receive.Timestamp)
                                            {
                                                //Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " has been kicked for speedhacking!"));
                                                Console.WriteLine(user.Name + " speedhacking. Old stamp > new stamp!");
                                                //user.Disconnect(true);
                                                return;
                                            }
                                            if (!user.ContainsFlag1(Effect1.Cyclone) && !user.ContainsFlag2(Effect2.Oblivion) && Native.timeGetTime() - user.CycloneEnded  >= 1000)
                                            {
                                                if (receive.Timestamp - user.LastClientJump <= 400)//Client says less than 500 ms
                                                {
                                                    //Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " has been kicked for speedhacking!"));
                                                    Console.WriteLine(user.Name + " speedhacking. CLIENT stamp is wrong "+(receive.Timestamp - user.LastClientJump));
                                                    //user.Disconnect(true);
                                                    return;
                                                }/*
                                                else if (Native.timeGetTime() - user.LastServerJump <= 200)//SERVER says less than 300 ms)
                                                {
                                                    Kernel.SendToServer(new Packet.TalkPacket(ChatType.Center, user.Name + " has been kicked for speedhacking!"));
                                                    Console.WriteLine(user.Name + " speedhacking. SERVER stamp is wrong " + (Native.timeGetTime() - user.LastServerJump));
                                                    user.Disconnect(true);
                                                    return;
                                                }*/
                                            }


                                            user.AttackPacket = null;
                                            if (user.ContainsFlag1(Effect1.Riding))
                                            {
                                                ushort take = (ushort)(1.5F * (Calculations.Movement.GetDistance(user.X, user.Y, receive.Data1Low, receive.Data1High) / 2));
                                                if (user.Vigor >= take)
                                                {
                                                    user.Vigor -= take;
                                                    user.Send(Packet.SteedVigor.Create(user.Vigor));
                                                }
                                                else
                                                {
                                                    user.RemoveFlag1(Effect1.Riding);
                                                    return;
                                                }
                                            }
                                            user.Action = ConquerAction.None;
                                            var jumpX = receive.Data1Low;
                                            var jumpY = receive.Data1High;
                                            user.SendToScreen(receive, true);
                                            //TODO Previous X/Y for callback
                                            if (user.Map.ValidTranslation(user.Location, new Core.Space.Point(jumpX, jumpY)))
                                            {
                                                user.LastJump = Native.timeGetTime();
                                                user.LastClientJump = receive.Timestamp;
                                                user.LastServerJump = Native.timeGetTime();
                                                user.SetLocation(jumpX, jumpY);
                                                user.UpdateBroadcastSet();
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region Away from Keyboard
                                    case DataAction.AwayFromKeyboard:
                                        {
                                            if (receive.Id == user.UID) // prevent someone from setting another players away status LOL
                                            {
                                                user.Away = receive.Data1 != 0;
                                                user.SendToScreen(receive, false);
                                            }
                                            break;
                                        }
                                    #endregion
                                    #region [148] QueryFriendInfo

                                    case DataAction.QueryFriendInfo:
                                        {
                                            if (user != null)
                                            {
                                                if (user.GetFriend(receive.Data1) != null)
                                                {
                                                    Player target;
                                                    Kernel.Clients.TryGetValue(receive.Data1, out target);

                                                    if (target != null)
                                                    {
                                                        user.Send(FriendInfoPacket.Create(FriendInfoAction.FriendInfo,
                                                                                          target.UID, target.Mesh,
                                                                                          target.Level, target.Profession,
                                                                                          target.PkPoints, 0, target.Spouse));
                                                    }
                                                }
                                            }
                                            break;
                                        }

                                    #endregion
                                    #region [408] QueryStatInfo
                                    case DataAction.QueryStatInfo:
                                        {
                                            user.Send(StatWindow.Create(user));
                                            break;
                                        }
                                    #endregion
                                    #region Create Booth
                                    case DataAction.CreateBooth:
                                        if (user.Shop == null)
                                            user.Shop = new PlayerShop(user);
                                        if (user.Shop.Vending)
                                        {
                                            Console.WriteLine("Already vending!");
                                            user.Shop.StopVending();
                                        }
                                        user.Shop.StartVending();
                                        receive.Data1 = user.Shop.Carpet.UID;
                                        user.Send(receive);
                                        break;
                                    #endregion
                                    default:
                                        Kernel.WriteLine("Error: unhandled general data type: " + receive.Action);
                                        break;
                                }
                                break;
                            }
                        #endregion

                        default:
                            {
                                Kernel.WriteLine("Unhandled packet type[{0}] from player[{1}]\n", type, user.Name);
                                break;
                            }
                    }
                }
            }
            catch (Exception p) { Kernel.WriteLine(p); }
        }

        private void HandleNobility(Nobility2Packet packet)
        {
            switch (packet.Action)
            {
                case NobilityAction.Donate:
                    {
                        var useCP = packet.Unknown16 != 0;
                        NobilityManager.PlayerDonate(user, packet.Data1Low, useCP);
                        break;
                    }
                case NobilityAction.List:
                    {
                        NobilityManager.SendNobilityPage(user, packet.Data1LowLow);
                        break;
                    }
                case NobilityAction.QueryRemainingSilver:
                    {
                        user.Send(Nobility2Packet.CreateRemaining((NobilityType)packet.Data1Low, user.NobilityDonation));
                        break;
                    }
            }
        }
    }
}
