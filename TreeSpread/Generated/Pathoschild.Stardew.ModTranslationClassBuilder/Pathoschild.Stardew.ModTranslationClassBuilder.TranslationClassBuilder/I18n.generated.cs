#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace TreeSpread
{
    /// <summary>Get translations from the mod's <c>i18n</c> folder.</summary>
    /// <remarks>This is auto-generated from the <c>i18n/default.json</c> file when the project is compiled.</remarks>
    [GeneratedCode("TextTemplatingFileGenerator", "1.0.0")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Deliberately named for consistency and to match translation conventions.")]
    internal static class I18n
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's translation helper.</summary>
        private static ITranslationHelper? Translations;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="translations">The mod's translation helper.</param>
        public static void Init(ITranslationHelper translations)
        {
            I18n.Translations = translations;
        }

        /// <summary>Get a translation equivalent to "Seed Chance".</summary>
        public static string SeedChanceName()
        {
            return I18n.GetByKey("SeedChanceName");
        }

        /// <summary>Get a translation equivalent to "Chance that a tree will have a seed. Normally this is 0.05 (=5%).".</summary>
        public static string SeedChanceTooltip()
        {
            return I18n.GetByKey("SeedChanceTooltip");
        }

        /// <summary>Get a translation equivalent to "Only Prevent Tapped".</summary>
        public static string OnlyPreventTappedName()
        {
            return I18n.GetByKey("OnlyPreventTappedName");
        }

        /// <summary>Get a translation equivalent to "Whether only tapped trees are prevented from spreading.".</summary>
        public static string OnlyPreventTappedTooltip()
        {
            return I18n.GetByKey("OnlyPreventTappedTooltip");
        }

        /// <summary>Get a translation equivalent to "Retain Seed".</summary>
        public static string RetainSeedName()
        {
            return I18n.GetByKey("RetainSeedName");
        }

        /// <summary>Get a translation equivalent to "Whether the tree should keep its seed during the night, to compensate for trees not spreading. Vanilla SDV removes seeds during the night.".</summary>
        public static string RetainSeedTooltip()
        {
            return I18n.GetByKey("RetainSeedTooltip");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a translation by its key.</summary>
        /// <param name="key">The translation key.</param>
        /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
        private static Translation GetByKey(string key, object? tokens = null)
        {
            if (I18n.Translations == null)
                throw new InvalidOperationException($"You must call {nameof(I18n)}.{nameof(I18n.Init)} from the mod's entry method before reading translations.");
            return I18n.Translations.Get(key, tokens);
        }
    }
}

