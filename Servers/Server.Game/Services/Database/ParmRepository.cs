using Database.DataModel.Enums;
using Database.DataModel.Models;
using Database.Fnl.Parm;
using Server.Game.Models.Game;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Services.Database
{
    /// <summary>
    ///     Database balance service
    /// </summary>
    public class ParmRepository
    {
        private readonly DBParmMappingService _parmMappingService;

        // Таблица опыта по возрастанию уровня, снятая при загрузке парма: по ней подбирается
        // строка уровня, которого в таблице нет (см. GetExpByLvl). Порядок нужен и самому поиску,
        // и потолку MaxExpLevel, а таблица за время жизни сервера не меняется
        private readonly List<ExpRow> _orderedExps;
        private readonly ExpRow _highestExpRow;

        public List<Abnormal> Abnormals { get; }
        public List<AbnormalAdd> AbnormalAdds { get; }
        public List<AbnormalResist> AbnormalResists { get; }
        public List<Attribute> Attributes { get; }
        public List<AttributeAdd> AttributeAdds { get; }
        public List<AttributeResist> AttributeResists { get; }
        public List<BeadEffect> BeadEffects { get; }
        public List<ItemPanalty> ItemPanalties { get; }
        public List<Item> Items { get; }
        public List<Module> Modules { get; }
        public List<Protect> Protects { get; }
        public List<Skill> Skills { get; }
        public List<Slain> Slains { get; }

        public List<DropItem> DropItems { get; }
        public List<DropGroup> DropGroups { get; }
        public List<MonsterDrop> MonsterDrops { get; }
        public List<MonsterSpotGroup> MonsterSpotGroups { get; }
        public List<MonsterSpot> MonsterSpots { get; }
        public List<Monster> Monsters { get; }

        public List<ExpRow> Exps { get; set; }

        /// <summary>
        ///     Потолок таблицы опыта — уровень последней её строки. Ноль, когда таблица пуста
        /// </summary>
        public short MaxExpLevel { get; }

        public ParmRepository(IFnlParmReferenceRepository parmReferenceRepository, DBParmMappingService parmMappingService)
        {
            _parmMappingService = parmMappingService;

            // Справочники, у которых есть собственная процедура FNLParm — грузятся готовыми POCO
            Abnormals = parmReferenceRepository.GetAbnormals().ToList();
            AbnormalAdds = parmReferenceRepository.GetAbnormalAdds().ToList();
            AbnormalResists = parmReferenceRepository.GetAbnormalResists().ToList();
            Skills = parmReferenceRepository.GetSkills().ToList();
            Items = parmReferenceRepository.GetItems().ToList();
            DropItems = parmReferenceRepository.GetDropItems().ToList();
            DropGroups = parmReferenceRepository.GetDropGroups().ToList();
            MonsterSpotGroups = parmReferenceRepository.GetMonsterSpotGroups().ToList();
            MonsterSpots = parmReferenceRepository.GetMonsterSpots().ToList();
            Monsters = parmReferenceRepository.GetMonsters().ToList();
            Exps = parmReferenceRepository.GetExps().ToList();

            _orderedExps = Exps.OrderBy(e => e.Level).ToList();
            _highestExpRow = _orderedExps.LastOrDefault();
            MaxExpLevel = _highestExpRow != null ? _highestExpRow.Level : (short)0;

            var monsterRoles = parmReferenceRepository.GetMonsterRoles();

            // Справочники без отдельной процедуры FNLParm остаются пустыми: связочные стабы,
            // которые ссылались бы на них, сохраняют только свой Id (мастер-объекта нет)
            Attributes = new List<Attribute>();
            AttributeAdds = new List<AttributeAdd>();
            AttributeResists = new List<AttributeResist>();
            BeadEffects = new List<BeadEffect>();
            ItemPanalties = new List<ItemPanalty>();
            Modules = new List<Module>();
            Protects = new List<Protect>();
            Slains = new List<Slain>();

            // Связи скиллов: стаб-строки (только Id) заменяются мастер-объектами справочника
            foreach (var skill in Skills)
            {
                var skillAbnormalIds = skill.Abnormals.Select(a => a.Id).ToList();
                skill.Abnormals = Abnormals.Where(a => skillAbnormalIds.Contains(a.Id)).ToList();

                // Attributes/Slains не имеют процедуры-справочника — стабы (только Id) сохраняются
            }

            // Связи предметов: стабы заменяются мастер-объектами по Id из соответствующего справочника
            foreach (var item in Items)
            {
                var itemSkillIds = item.Skills.Select(s => s.Id).ToList();
                item.Skills = Skills.Where(s => itemSkillIds.Contains(s.Id)).ToList();

                var itemAbnormalAddIds = item.AbnormalAdds.Select(a => a.Id).ToList();
                item.AbnormalAdds = AbnormalAdds.Where(a => itemAbnormalAddIds.Contains(a.Id)).ToList();

                var itemAbnormalResistIds = item.AbnormalResists.Select(a => a.Id).ToList();
                item.AbnormalResists = AbnormalResists.Where(a => itemAbnormalResistIds.Contains(a.Id)).ToList();

                // AttributeAdds/AttributeResists/Slains/Protects не имеют процедуры-справочника —
                // стабы (только Id) сохраняются как есть
            }

            // Дроп-предметы -> мастер-предмет по ItemId
            foreach (var dropItem in DropItems)
            {
                dropItem.Item = Items.FirstOrDefault(i => i.Id == dropItem.ItemId);
            }

            // Позиции дроп-групп -> мастер дроп-предмета по DropItemId
            foreach (var dropGroup in DropGroups)
            {
                foreach (var dropGroupItem in dropGroup.Items)
                {
                    dropGroupItem.DropItem = DropItems.FirstOrDefault(i => i.Id == dropGroupItem.DropItemId);
                }
            }

            // Споты -> группы спотов по GroupId
            foreach (var monsterSpot in MonsterSpots)
            {
                monsterSpot.SpotGroup = MonsterSpotGroups.Where(msg => msg.Id == monsterSpot.GroupId).ToList();
            }

            // Связи мобов
            foreach (var monster in Monsters)
            {
                var mAbnormalAddIds = monster.AbnormalAdds.Select(a => a.Id).ToList();
                monster.AbnormalAdds = AbnormalAdds.Where(a => mAbnormalAddIds.Contains(a.Id)).ToList();

                var mAbnormalResistIds = monster.AbnormalResists.Select(a => a.Id).ToList();
                monster.AbnormalResists = AbnormalResists.Where(a => mAbnormalResistIds.Contains(a.Id)).ToList();

                // AttributeAdds/AttributeResists/Slains/Protects не имеют процедуры-справочника —
                // стабы (только Id) сохраняются как есть

                // Дропы мобов уже несут DropGroupId/Percent — досшиваем мастер дроп-группы по Id
                foreach (var drop in monster.Drops)
                {
                    drop.DropGroup = DropGroups.FirstOrDefault(dg => dg.DropGroupId == drop.DropGroupId);
                }

                monster.Roles = monsterRoles.Where(mr => mr.MonsterId == monster.Id).Select(mr => mr.Role).ToList();
                //monster.Spots = MonsterSpots.Where(ms => ms.MonsterId == monster.Id).ToList();
            }

            // Плоский список дропов мобов (раньше грузился отдельной таблицей)
            MonsterDrops = Monsters.SelectMany(m => m.Drops).ToList();
        }

        #region Character
        /// <summary>
        ///     Get character position by class
        /// </summary>
        /// <param name="characterType"></param>
        /// <returns></returns>
        public GCharacterPosition GetCharacterPositionByClass(CharacterTypeEnum characterType)
        {
            // В FNLParm нет процедуры для стартовых позиций по классу (см. T5), а единственный вызов
            // этого метода закомментирован (CharacterHandler). Возвращаем пустую позицию-заглушку с
            // сохранением сигнатуры; при появлении процедуры сюда встанет чтение из предзагруженного кэша
            GCharacterPosition characterPositionGame = new GCharacterPosition
            {
                Class = characterType
            };

            return characterPositionGame;
        }
        #endregion

        #region Exp
        /// <summary>
        ///     Get exp by lvl. Уровня может не быть в таблице: тогда берётся ближайшая строка
        ///     сверху, а выше потолка таблицы — последняя строка (на потолке уровень не растёт —
        ///     см. ExpSystem.KillUnit). Пустая таблица даёт нулевой порог, а не исключение
        /// </summary>
        /// <param name="level"></param>
        public GExp GetExpByLvl(long level)
        {
            GExp expGame = new GExp();

            ExpRow exp = FindExpRow(level);

            if (exp == null)
            {
                return expGame;
            }

            _parmMappingService.MapExpGame(expGame, exp);

            return expGame;
        }

        /// <summary>
        ///     Строка таблицы опыта по уровню: точная, а если такой нет — ближайшая с уровнем выше
        ///     (дыра в середине таблицы не должна отдавать порог первого уровня и дарить мгновенный
        ///     ап). Выше потолка — последняя строка, на пустой таблице — null. Поиск двоичный по
        ///     упорядоченной копии таблицы: метод зовётся на каждом апе и при входе в мир
        /// </summary>
        /// <param name="level"></param>
        private ExpRow FindExpRow(long level)
        {
            int low = 0;
            int high = _orderedExps.Count - 1;
            ExpRow nearestAbove = null;

            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                ExpRow row = _orderedExps[middle];

                if (row.Level == level)
                {
                    return row;
                }

                if (row.Level > level)
                {
                    nearestAbove = row;
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return nearestAbove ?? _highestExpRow;
        }
        #endregion

        #region Item
        /// <summary>
        ///     Get item by id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Item GetItemById(int itemId)
        {
            Item item = Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                return null;
            }

            return item;
        }

        /// <summary>
        ///     Get GItem by id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public GItem GetGItemById(int itemId)
        {
            Item item = Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                return null;
            }

            GItem itemGame = new GItem(item);

            _parmMappingService.MapItemGame(itemGame, item);

            return itemGame;
        }

        #endregion

        #region Monster
        public List<GMonster> GetAllMonsters()
        {
            List<GMonster> monsters = new List<GMonster>();

            foreach (var monster in Monsters)
            {
                var monsterGame = new GMonster();
                _parmMappingService.MapMonstertGame(monsterGame, monster);
                monsters.Add(monsterGame);
            }

            return monsters;
        }

        /// <summary>
        ///     Get unit by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GMonster GetGMonsterById(int id)
        {
            GMonster monsterGame = new GMonster();

            Monster monster = Monsters.First(u => u.Id == id);
            _parmMappingService.MapMonstertGame(monsterGame, monster);

            return monsterGame;
        }

        /// <summary>
        ///     Get unit by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ParmMonster GetMonsterById(int id)
        {
            Monster monster = Monsters.First(u => u.Id == id);

            var monsterGame = new ParmMonster(monster);

            return monsterGame;
        }

        /// <summary>
        ///     Get unit by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<MonsterSpot> GetAllMonsterSpots()
        {
            var monsterSpots = MonsterSpots.ToList();

            return monsterSpots;
        }
        #endregion
    }
}
