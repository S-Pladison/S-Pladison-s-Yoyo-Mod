namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Тип всплывающей строки в описании предмета.
    /// </summary>
    public enum VanillaTooltipLine : int
    {
        /// <summary>Строка не определена.</summary>
        Undefined,
        /// <summary>Название предмета.</summary>
        ItemName,
        /// <summary>Указывает, избран ли предмет.</summary>
        Favorite,
        /// <summary>Описание избранного предмета.</summary>
        FavoriteDesc,
        /// <summary>Предупреждение о том, что этот предмет нельзя поместить сам в себя (используется для Money Trough, Void Bag/Vault).</summary>
        NoTransfer,
        /// <summary>Указывает, находится ли предмет в социальной ячейке.</summary>
        Social,
        /// <summary>Описание нахождения предмета в социальной ячейке.</summary>
        SocialDesc,
        /// <summary>Значение и тип урона оружия.</summary>
        Damage,
        /// <summary>Шанс нанесения критического удара оружием.</summary>
        CritChance,
        /// <summary>Скорость использования оружия.</summary>
        Speed,
        /// <summary>Указывает, не зависит ли предмет от бонусов к скорости атаки (добавлено в tModLoader).</summary>
        NoSpeedScaling,
        /// <summary>Множитель, применяемый предметом к бонусам скорости атаки (добавлено в tModLoader).</summary>
        SpecialSpeedScaling,
        /// <summary>Сила отбрасывания оружия.</summary>
        Knockback,
        /// <summary>Указывает силу рыбалки удочки.</summary>
        FishingPower,
        /// <summary>Указывает, что для удочки требуется наживка.</summary>
        NeedsBait,
        /// <summary>Сила наживки.</summary>
        BaitPower,
        /// <summary>Указывает, что предмет можно экипировать.</summary>
        Equipable,
        /// <summary>Указывает, какой предмет потребляет жезл для размещения плитки.</summary>
        WandConsumes,
        /// <summary>Указывает, что предмет является квестовым.</summary>
        Quest,
        /// <summary>Указывает, что предмет декоративный.</summary>
        Vanity,
        /// <summary>Показывает, сколько защиты даёт предмет при экипировке.</summary>
        Defense,
        /// <summary>Мощность кирки предмета.</summary>
        PickPower,
        /// <summary>Мощность топора предмета.</summary>
        AxePower,
        /// <summary>Мощность молота предмета.</summary>
        HammerPower,
        /// <summary>Насколько дальше предмет может дотянуться по сравнению с обычными.</summary>
        TileBoost,
        /// <summary>Сколько здоровья восстанавливает предмет при использовании.</summary>
        HealLife,
        /// <summary>Сколько маны восстанавливает предмет при использовании.</summary>
        HealMana,
        /// <summary>Сколько маны расходует предмет при использовании.</summary>
        UseMana,
        /// <summary>Указывает, можно ли разместить предмет.</summary>
        Placeable,
        /// <summary>Указывает, является ли предмет боеприпасом.</summary>
        Ammo,
        /// <summary>Указывает, является ли предмет расходуемым.</summary>
        Consumable,
        /// <summary>Указывает, можно ли использовать предмет в крафте.</summary>
        Material,
        /// <summary>Линия подсказки для предмета. Предмет может иметь несколько таких линий. Постфикс 0 указывает на то, что строка является первой, 1 - второй и т.д.</summary>
        Tooltip,
        /// <summary>Предупреждение, что предмет нельзя использовать без эфирной маны до победы над Eternia Crystal.</summary>
        EtherianManaWarning,
        /// <summary>В режиме эксперта указывает, что еда увеличивает восстановление здоровья.</summary>
        WellFedExpert,
        /// <summary>Указывает длительность эффекта баффа от предмета.</summary>
        BuffTime,
        /// <summary>Логотип One Drop для йо-йо. Это специальная строка подсказки без текста.</summary>
        OneDropLogo,
        /// <summary>Модификатор урона префикса.</summary>
        PrefixDamage,
        /// <summary>Модификатор скорости использования префикса.</summary>
        PrefixSpeed,
        /// <summary>Модификатор шанса критического удара префикса.</summary>
        PrefixCritChance,
        /// <summary>Модификатор расхода маны префикса.</summary>
        PrefixUseMana,
        /// <summary>Модификатор размера оружия ближнего боя префикса.</summary>
        PrefixSize,
        /// <summary>Модификатор скорости полёта снарядов префикса.</summary>
        PrefixShootSpeed,
        /// <summary>Модификатор отбрасывания префикса.</summary>
        PrefixKnockback,
        /// <summary>Модификатор защиты аксессуара.</summary>
        PrefixAccDefense,
        /// <summary>Модификатор максимальной маны аксессуара.</summary>
        PrefixAccMaxMana,
        /// <summary>Модификатор шанса критического удара аксессуара.</summary>
        PrefixAccCritChance,
        /// <summary>Модификатор урона аксессуара.</summary>
        PrefixAccDamage,
        /// <summary>Модификатор скорости передвижения аксессуара.</summary>
        PrefixAccMoveSpeed,
        /// <summary>Модификатор скорости атаки ближнего боя аксессуара.</summary>
        PrefixAccMeleeSpeed,
        /// <summary>Описание бонуса комплекта брони.</summary>
        SetBonus,
        /// <summary>Указывает, что предмет из режима эксперта.</summary>
        Expert,
        /// <summary>Указывает, что предмет эксклюзивен для режима мастера.</summary>
        Master,
        /// <summary>Указывает, сколько ещё предметов нужно изучить, чтобы разблокировать дублирование в режиме путешествия.</summary>
        JourneyResearch,
        /// <summary>Указывает, был ли предмет изменён модами и какими, при удерживании Shift (добавлено в tModLoader).</summary>
        ModifiedByMods,
        /// <summary>Любые заметки бестиария, отображаемые при наведении на предметы в бестиарии.</summary>
        BestiaryNotes,
        /// <summary>Указывает альтернативную цену предмета.</summary>
        SpecialPrice,
        /// <summary>Указывает цену предмета.</summary>
        Price
    }
}