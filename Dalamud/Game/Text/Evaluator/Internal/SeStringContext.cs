using System.Globalization;

using Dalamud.Utility;

using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// A context wrapper used in <see cref="SeStringEvaluator"/>.
/// </summary>
internal ref struct SeStringContext
{
    /// <summary>
    /// The <see cref="SeStringBuilder"/> to append text and macros to.
    /// </summary>
    internal ref SeStringBuilder Builder;

    /// <summary>
    /// A list of local parameters.
    /// </summary>
    internal Span<SeStringParameter> LocalParameters;

    /// <summary>
    /// The target language, used for sheet lookups.
    /// </summary>
    internal ClientLanguage Language;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeStringContext"/> struct.
    /// </summary>
    /// <param name="builder">The <see cref="SeStringBuilder"/> to append text and macros to.</param>
    /// <param name="localParameters">A list of local parameters.</param>
    /// <param name="language">The target language, used for sheet lookups.</param>
    internal SeStringContext(ref SeStringBuilder builder, Span<SeStringParameter> localParameters, ClientLanguage language)
    {
        this.Builder = ref builder;
        this.LocalParameters = localParameters;
        this.Language = language;
    }

    /// <summary>
    /// Gets the <see cref="System.Globalization.CultureInfo"/> of the current target <see cref="Language"/>.
    /// </summary>
    internal CultureInfo CultureInfo => Localization.GetCultureInfoFromLangCode(this.Language.ToCode());

    /// <summary>
    /// Tries to get a number from the local parameters at the specified index.
    /// </summary>
    /// <param name="index">The index in the <see cref="LocalParameters"/> list.</param>
    /// <param name="value">The local parameter number.</param>
    /// <returns><c>true</c> if the local parameters list contained a parameter at given index, <c>false</c> otherwise.</returns>
    internal bool TryGetLNum(int index, out uint value)
    {
        if (index >= 0 && this.LocalParameters.Length > index && this.LocalParameters[index] is SeStringParameter { } val)
        {
            value = val.UIntValue;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Tries to get a string from the local parameters at the specified index.
    /// </summary>
    /// <param name="index">The index in the <see cref="LocalParameters"/> list.</param>
    /// <param name="value">The local parameter string.</param>
    /// <returns><c>true</c> if the local parameters list contained a parameter at given index, <c>false</c> otherwise.</returns>
    internal bool TryGetLStr(int index, out ReadOnlySeString value)
    {
        if (index >= 0 && this.LocalParameters.Length > index && this.LocalParameters[index] is SeStringParameter { } val)
        {
            value = val.StringValue;
            return true;
        }

        value = default;
        return false;
    }
}
