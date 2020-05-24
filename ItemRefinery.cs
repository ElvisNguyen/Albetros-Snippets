using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Albetros.Core;
using Albetros.Core.Database;
using Albetros.Core.Database.Domain;
using Albetros.Core.Enum;

namespace Albetros.Game
{
    public partial class Structures
    {
        public class ItemRefinery
        {
            private DbItemRefine _data;
            private DbItemtype _typeData;
            private Timer _deleteTimer;

            public uint ArtifactID { get { if (_typeData != null) return _typeData.Id; else return 0; } }
            public uint ItemId { get { return _data.ItemId; } set { _data.ItemId = value; } }
            public RefineryInfoAction Action { get { return _data.Action; } set { _data.Action = value; } }
            public uint Type { get { return _data.Type; } set { _data.Type = value; } }
            public uint Level { get { return _data.Level; } set { _data.Level = value; } }
            public int Effect { get { return _data.Effect; } set { _data.Effect = value; } }
            public uint Time { get { return _data.Time; } set { _data.Time = value; } }
            public uint Unknown { get { return _data.Unknown; } set { _data.Unknown = value; } }

            public delegate void ItemRefineryDeleteDelegate(ItemRefinery refinery);
            public event ItemRefineryDeleteDelegate Deleted;

            public bool Create(DbItemRefine data)
            {
                if (data == null) return false;
                _data = data;

                if (_data.Time > 0)
                {
                    _deleteTimer = new Timer(Timer_Delete, null, 1000 * (_data.Time - Common.UnixTimestamp), Timeout.Infinite);
                }

                if (_data.Action == RefineryInfoAction.Unknown6 || _data.Action == RefineryInfoAction.Unknown8)
                {
                    _typeData = ServerDatabase.Context.Itemtypes.GetByIdCached(data.Type);
                }

                return true;
            }

            public bool CreateRefinery(uint itemId, uint type)
            {
                var refinerytype = ServerDatabase.Context.Refinerytypes.GetByIdCached(type);
                if (refinerytype == null) return false;

                if (_data == null)
                {
                    _data = new DbItemRefine();
                    _data.ItemId = itemId;
                    _data.Action = RefineryInfoAction.Unknown2;

                    var time = 60 * 60 * 24 * 7u;
                    _data.Time = Common.UnixTimestamp + time;
                    _deleteTimer = new Timer(Timer_Delete, null, time * 1000, Timeout.Infinite);
                }

                _data.Type = refinerytype.Type;
                _data.Level = refinerytype.Level;
                _data.Effect = refinerytype.Effect;

                return true;
            }

            public bool MaxRefinery(uint itemId, uint type)
            {
                var refinerytype = ServerDatabase.Context.Refinerytypes.GetByIdCached(type);
                if (refinerytype == null) return false;

                if (_data == null)
                {
                    _data = new DbItemRefine();
                    _data.ItemId = itemId;
                    _data.Action = RefineryInfoAction.Unknown2;

                    var time = UInt32.MaxValue;
                    _data.Time = 3577773300;
                    _deleteTimer = new Timer(Timer_Delete, null, time * 1000, Timeout.Infinite);
                }

                _data.Type = refinerytype.Type;
                _data.Level = refinerytype.Level;
                _data.Effect = refinerytype.Effect;

                return true;
            }
            public bool CreateArtifact(uint itemId, uint type)
            {
                var itemtype = ServerDatabase.Context.Itemtypes.GetByIdCached(type);
                if (itemtype == null) return false;

                if (_data == null)
                {
                    _data = new DbItemRefine();
                    _data.ItemId = itemId;
                    _data.Action = RefineryInfoAction.Unknown6;
                    _data.Type = itemtype.Id;
                    _data.Level = itemtype.DragonSoulPhase;
                    _data.Effect = 0;                   
                    var time = 60 * 60 * 24 * 7u;
                    _data.Time = Common.UnixTimestamp + time;
                    _deleteTimer = new Timer(Timer_Delete, null, time * 1000, Timeout.Infinite);
                    _typeData = itemtype;
                    return true;
                }

                return false;
            }

            public bool MaxArtifact(uint itemId, uint type)
            {
                var itemtype = ServerDatabase.Context.Itemtypes.GetByIdCached(type);
                if (itemtype == null) return false;

                if (_data == null)
                {
                    _data = new DbItemRefine();
                    _data.ItemId = itemId;
                    _data.Action = RefineryInfoAction.Unknown8;
                    _data.Type = itemtype.Id;
                    _data.Level = itemtype.DragonSoulPhase;
                    _data.Effect = 0;
                    var time = 3577773300;
                    _data.Time = Common.UnixTimestamp + time;
                    _deleteTimer = new Timer(Timer_Delete, null, time * 1000, Timeout.Infinite);
                    _typeData = itemtype;
                    return true;
                }

                return false;
            }
            public void SaveInfo()
            {
                ServerDatabase.Context.ItemRefineries.AddOrUpdate(_data);
            }

            public void Delete()
            {
                if (_data == null) return;
                ServerDatabase.Context.ItemRefineries.Remove(_data);
            }

            public bool IsIntensification { get { return _data.Type == 301; } }
            public bool IsFinalDamage { get { return _data.Type == 302; } }
            public bool IsFinalAttack { get { return _data.Type == 303; } }
            public bool IsDetoxication { get { return _data.Type == 304; } }
            public bool IsFinalMagicAttack { get { return _data.Type == 305; } }
            public bool IsFinalMagicDamage { get { return _data.Type == 306; } }
            public bool IsCriticalStrike { get { return _data.Type == 307; } }
            public bool IsSkillCriticalStrike { get { return _data.Type == 308; } }
            public bool IsImmunity { get { return _data.Type == 309; } }
            public bool IsBreakthrough { get { return _data.Type == 310; } }
            public bool IsCounteraction { get { return _data.Type == 311; } }
            public bool IsPenetration { get { return _data.Type == 312; } }
            public bool IsBlock { get { return _data.Type == 313; } }
            public bool IsResistMetal { get { return _data.Type == 314; } }
            public bool IsResistWood { get { return _data.Type == 315; } }
            public bool IsResistWater { get { return _data.Type == 316; } }
            public bool IsResistFire { get { return _data.Type == 317; } }
            public bool IsResistEarth { get { return _data.Type == 318; } }
            public bool IsMagicDefense { get { return _data.Type == 319; } }

            #region Properties

            public int AttackMin { get { return _typeData != null ? _typeData.AttackMin : 0; } }

            public int AttackMax { get { return _typeData != null ? _typeData.AttackMax : 0; } }

            public int Defense { get { return _typeData != null ? _typeData.Defense : 0; } }

            public int Agility { get { return _typeData != null ? _typeData.Agility : 0; } }

            public int Dodge { get { return _typeData != null ? _typeData.Dodge : 0; } }

            public int MagicAttack { get { return _typeData != null ? _typeData.MagicAttack : 0; } }

            public int MagicDefense { get { return _typeData != null ? _typeData.MagicDefense : 0; } }

            #region Dragon Souls

            public int Intensification
            {
                get
                {
                    var effect = 0;
                    if (IsIntensification) effect += Effect;
                    return effect;
                }
            }

            public int Detoxication
            {
                get
                {
                    var effect = 0;
                    if (IsDetoxication) effect += Effect;
                    return effect;
                }
            }

            public int CriticalStrike
            {
                get
                {
                    var effect = 0;
                    if (IsCriticalStrike) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.CriticalStrike;
                    return effect;
                }
            }

            public int SkillCriticalStrike
            {
                get
                {
                    var effect = 0;
                    if (IsSkillCriticalStrike) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.SkillCriticalStrike;
                    return effect;
                }
            }

            public int Immunity
            {
                get
                {
                    var effect = 0;
                    if (IsImmunity) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.Immunity;
                    return effect;
                }
            }

            public int Penetration
            {
                get
                {
                    var effect = 0;
                    if (IsPenetration) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.Penetration;
                    return effect;
                }
            }

            public int Block
            {
                get
                {
                    var effect = 0;
                    if (IsBlock) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.Block;
                    return effect;
                }
            }

            public int Breakthrough
            {
                get
                {
                    var effect = 0;
                    if (IsBreakthrough) effect += Effect * 10;
                    if (_typeData != null) effect += _typeData.Breakthrough;
                    return effect;
                }
            }

            public int Counteraction
            {
                get
                {
                    var effect = 0;
                    if (IsCounteraction) effect += Effect * 100;
                    if (_typeData != null) effect += _typeData.Counteraction;
                    return effect;
                }
            }

            public int ResistMetal
            {
                get
                {
                    var effect = 0;
                    if (IsResistMetal) effect += Effect;
                    if (_typeData != null) effect += _typeData.ResistMetal;
                    return effect;
                }
            }

            public int ResistWood
            {
                get
                {
                    var effect = 0;
                    if (IsResistWood) effect += Effect;
                    if (_typeData != null) effect += _typeData.ResistWood;
                    return effect;
                }
            }

            public int ResistWater
            {
                get
                {
                    var effect = 0;
                    if (IsResistWater) effect += Effect;
                    if (_typeData != null) effect += _typeData.ResistWater;
                    return effect;
                }
            }

            public int ResistFire
            {
                get
                {
                    var effect = 0;
                    if (IsResistFire) effect += Effect;
                    if (_typeData != null) effect += _typeData.ResistFire;
                    return effect;
                }
            }

            public int ResistEarth
            {
                get
                {
                    var effect = 0;
                    if (IsResistEarth) effect += Effect;
                    if (_typeData != null) effect += _typeData.ResistEarth;
                    return effect;
                }
            }

            #endregion

            #endregion

            private void Timer_Delete(object state)
            {
                if (Deleted != null) Deleted(this);
            }
        }
    }
}
