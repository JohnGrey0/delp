namespace Delp.Core.Tools.TextProcessing;

/// <summary>
/// Hand-implemented classic Porter stemming algorithm (M.F. Porter, 1980,
/// "An algorithm for suffix stripping"), following the paper's steps 1a
/// through 5b.
///
/// Two well-documented refinements from the algorithm's most widely used
/// reference implementation (NLTK's <c>PorterStemmer</c>) are included,
/// because without them the stemmer produces counter-intuitive results for
/// very common short words:
///  - Step 1a: a 4-letter word ending "ies" (e.g. "dies", "ties") keeps the
///    "ie" rather than collapsing to a single letter, so "dies" -&gt; "die"
///    instead of "di" (longer words are unaffected: "ponies" -&gt; "poni").
///  - Step 1c: "Y -&gt; I" applies whenever the preceding stem is more than
///    one letter and ends in a consonant, not only when the stem contains a
///    vowel. This lets "flying" -&gt; "fly" -&gt; "fli", conflating with "flies".
/// </summary>
public static class PorterStemmer
{
    /// <summary>
    /// Stems a single word. Operates on lowercase ASCII letters only; any
    /// input containing characters outside a-z (after lowercasing) is
    /// returned unchanged, and words of 2 letters or fewer are returned
    /// lowercased but otherwise untouched (per the algorithm's own m&gt;0
    /// gate on running any step at all).
    /// </summary>
    public static string Stem(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        var lower = word.ToLowerInvariant();
        foreach (var ch in lower)
        {
            if (ch < 'a' || ch > 'z')
                return word;
        }

        if (lower.Length <= 2)
            return lower;

        var ctx = new Context(lower);
        ctx.Step1A();
        ctx.Step1B();
        ctx.Step1C();
        ctx.Step2();
        ctx.Step3();
        ctx.Step4();
        ctx.Step5A();
        ctx.Step5B();
        return ctx.Result();
    }

    /// <summary>
    /// Mutable per-call algorithm state: a fixed-size character buffer plus
    /// the two index registers ("k" = last index of the current word, "j" =
    /// scratch index set by <see cref="Ends"/>) that the classic reference
    /// implementation uses. The buffer never needs to grow past the
    /// original word's length -- every replacement rule in the algorithm
    /// produces a result no longer than the suffix it replaces once the
    /// preceding removal in the same step is accounted for.
    /// </summary>
    private sealed class Context
    {
        private readonly char[] _b;
        private int _k;
        private int _j;

        public Context(string word)
        {
            _b = word.ToCharArray();
            _k = _b.Length - 1;
        }

        public string Result() => new(_b, 0, _k + 1);

        // ---- character classification -------------------------------------------------

        /// <summary>
        /// A letter is a consonant unless it is A/E/I/O/U, or it is a Y that
        /// is directly preceded by a consonant (Y is then treated as a
        /// vowel). A word-initial Y, or a Y preceded by a vowel, is a
        /// consonant (matches the paper's own TOY / SYZYGY examples).
        /// </summary>
        private bool Cons(int i) => _b[i] switch
        {
            'a' or 'e' or 'i' or 'o' or 'u' => false,
            'y' => i == 0 || !Cons(i - 1),
            _ => true,
        };

        /// <summary>Measure (m) of the stem b[0..upTo]: the number of VC repeats in [C](VC)^m[V].</summary>
        private int Measure(int upTo)
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > upTo) return n;
                if (!Cons(i)) break;
                i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > upTo) return n;
                    if (Cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > upTo) return n;
                    if (!Cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /// <summary>*v* — does b[0..upTo] contain a vowel?</summary>
        private bool ContainsVowel(int upTo)
        {
            for (int i = 0; i <= upTo; i++)
                if (!Cons(i))
                    return true;
            return false;
        }

        /// <summary>*d — does the stem end with a double consonant?</summary>
        private bool DoubleC(int idx)
        {
            if (idx < 1) return false;
            if (_b[idx] != _b[idx - 1]) return false;
            return Cons(idx);
        }

        /// <summary>*o — does the stem end cvc, where the second c is not W, X or Y?</summary>
        private bool Cvc(int i)
        {
            if (i < 2 || !Cons(i) || Cons(i - 1) || !Cons(i - 2))
                return false;
            var ch = _b[i];
            return ch != 'w' && ch != 'x' && ch != 'y';
        }

        // ---- suffix matching / replacement -----------------------------------------

        /// <summary>If the current word ends with <paramref name="s"/>, sets j to the index before it and returns true.</summary>
        private bool Ends(string s)
        {
            var l = s.Length;
            var o = _k - l + 1;
            if (o < 0) return false;
            for (var i = 0; i < l; i++)
                if (_b[o + i] != s[i])
                    return false;
            _j = _k - l;
            return true;
        }

        /// <summary>Replaces everything after j with <paramref name="s"/> and updates k.</summary>
        private void SetTo(string s)
        {
            var o = _j + 1;
            for (var i = 0; i < s.Length; i++)
                _b[o + i] = s[i];
            _k = _j + s.Length;
        }

        /// <summary>setto(s), but only when the stem before the matched suffix has m &gt; 0.</summary>
        private void R(string s)
        {
            if (Measure(_j) > 0)
                SetTo(s);
        }

        /// <summary>Truncates to j (removes the matched suffix) when the stem before it has m &gt; 1.</summary>
        private void TruncIfM1()
        {
            if (Measure(_j) > 1)
                _k = _j;
        }

        // ---- steps ------------------------------------------------------------------

        public void Step1A()
        {
            // NLTK extension: keep 4-letter "...ies" words as "...ie"
            // (dies -> die, ties -> tie) instead of collapsing to one letter.
            if (_k == 3 && Ends("ies"))
            {
                SetTo("ie");
                return;
            }

            if (Ends("sses")) _k -= 2;
            else if (Ends("ies")) SetTo("i");
            else if (Ends("ss")) { /* SS -> SS: unchanged */ }
            else if (Ends("s")) _k--;
        }

        public void Step1B()
        {
            if (Ends("eed"))
            {
                if (Measure(_j) > 0) _k--;
                return;
            }

            if ((Ends("ed") || Ends("ing")) && ContainsVowel(_j))
            {
                _k = _j;
                if (Ends("at")) SetTo("ate");
                else if (Ends("bl")) SetTo("ble");
                else if (Ends("iz")) SetTo("ize");
                else if (DoubleC(_k))
                {
                    var last = _b[_k];
                    if (last != 'l' && last != 's' && last != 'z')
                        _k--;
                }
                else if (Measure(_k) == 1 && Cvc(_k))
                {
                    SetTo("e");
                }
            }
        }

        public void Step1C()
        {
            if (!Ends("y")) return;

            // NLTK extension: apply whenever the stem before Y is more than
            // one letter and ends in a consonant (not only when it contains
            // a vowel), so "fly"/"try"/"spy" stem to "fli"/"tri"/"spi" and
            // conflate with "flies"/"tried"/"spied".
            if (_j > 0 && Cons(_j))
                _b[_k] = 'i';
        }

        public void Step2()
        {
            if (Ends("ational")) R("ate");
            else if (Ends("tional")) R("tion");
            else if (Ends("enci")) R("ence");
            else if (Ends("anci")) R("ance");
            else if (Ends("izer")) R("ize");
            else if (Ends("abli")) R("able");
            else if (Ends("alli")) R("al");
            else if (Ends("entli")) R("ent");
            else if (Ends("eli")) R("e");
            else if (Ends("ousli")) R("ous");
            else if (Ends("ization")) R("ize");
            else if (Ends("ation")) R("ate");
            else if (Ends("ator")) R("ate");
            else if (Ends("alism")) R("al");
            else if (Ends("iveness")) R("ive");
            else if (Ends("fulness")) R("ful");
            else if (Ends("ousness")) R("ous");
            else if (Ends("aliti")) R("al");
            else if (Ends("iviti")) R("ive");
            else if (Ends("biliti")) R("ble");
        }

        public void Step3()
        {
            if (Ends("icate")) R("ic");
            else if (Ends("ative")) R("");
            else if (Ends("alize")) R("al");
            else if (Ends("iciti")) R("ic");
            else if (Ends("ical")) R("ic");
            else if (Ends("ful")) R("");
            else if (Ends("ness")) R("");
        }

        public void Step4()
        {
            if (Ends("al")) TruncIfM1();
            else if (Ends("ance")) TruncIfM1();
            else if (Ends("ence")) TruncIfM1();
            else if (Ends("er")) TruncIfM1();
            else if (Ends("ic")) TruncIfM1();
            else if (Ends("able")) TruncIfM1();
            else if (Ends("ible")) TruncIfM1();
            else if (Ends("ant")) TruncIfM1();
            else if (Ends("ement")) TruncIfM1();
            else if (Ends("ment")) TruncIfM1();
            else if (Ends("ent")) TruncIfM1();
            else if (Ends("ion"))
            {
                if (_j >= 0 && (_b[_j] == 's' || _b[_j] == 't') && Measure(_j) > 1)
                    _k = _j;
            }
            else if (Ends("ou")) TruncIfM1();
            else if (Ends("ism")) TruncIfM1();
            else if (Ends("ate")) TruncIfM1();
            else if (Ends("iti")) TruncIfM1();
            else if (Ends("ous")) TruncIfM1();
            else if (Ends("ive")) TruncIfM1();
            else if (Ends("ize")) TruncIfM1();
        }

        public void Step5A()
        {
            if (!Ends("e")) return;
            var m = Measure(_j);
            if (m > 1 || (m == 1 && !Cvc(_j)))
                _k = _j;
        }

        public void Step5B()
        {
            if (Measure(_k) > 1 && DoubleC(_k) && _b[_k] == 'l')
                _k--;
        }
    }
}
