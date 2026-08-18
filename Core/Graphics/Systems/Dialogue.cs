using System;
using System.Collections.Generic;
using System.Text;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace TheExtraordinaryAdditions.Core.Graphics.Systems;

// Primarily from Fables
// TODO: Shaders? Opacity delegate?

public struct TextSnippet
{
    #region Character Data

    public static readonly float DefaultSize = 16f;
    public static readonly DynamicSpriteFont DefaultFont = FontAssets.MouseText.Value;

    public delegate Vector2 CharacterDisplacementDelegate(int character);

    public delegate Vector2 CharacterAppearDelegate(int character, float progress);

    public delegate Color LetterColorDelegate(int character, float globalProgress);

    public static readonly CharacterDisplacementDelegate NoDisplacement =
        _ => Vector2.Zero;

    public static CharacterDisplacementDelegate RandomDisplacement(float power) =>
        _ => Main.rand.NextVector2Circular(power, power);

    public static CharacterDisplacementDelegate WaveDisplacement(float power = 2.5f, bool horizontal = false) =>
        character =>
        {
            float sine = (float) Math.Sin(Main.GlobalTimeWrappedHourly * power + character * 0.8f) * power;
            return new(horizontal ? sine : 0, horizontal ? 0 : sine);
        };

    public static readonly CharacterAppearDelegate AppearSuddenly =
        (_, _) => Vector2.Zero;

    public static readonly CharacterAppearDelegate AppearFadingFromTop =
        (_, progress) => new(0, -MakePoly(1.6f).InFunction(1 - progress) * 16f);

    public static readonly CharacterAppearDelegate AppearFadingFromTopZipper =
        (character, progress) => new(0, -MakePoly(1.6f).InFunction(1 - progress) * 16f * (character % 2 == 1).ToInt());

    public static readonly CharacterAppearDelegate AppearFadingFromRight =
        (_, progress) => new(MakePoly(2.1f).InFunction(1 - progress) * 16f, 0f);

    #endregion

    #region Public Fields

    /// <summary>
    /// The actual text
    /// </summary>
    public string Content;

    /// <summary>
    /// The color to apply to the full text
    /// </summary>
    public readonly LetterColorDelegate TextColor;

    /// <summary>
    /// In milliseconds, how much to wait for the next character in progression
    /// </summary>
    public readonly float CharacterAppearDelay;

    public const float DefaultCharacterDelay = .025f;

    /// <summary>
    /// How the text should appear
    /// </summary>
    public readonly CharacterAppearDelegate TextAppear;

    /// <summary>
    /// A displacement of the position of every character
    /// </summary>
    public readonly CharacterDisplacementDelegate TextDisplacement;

    /// <summary>
    /// The size, in pixels, of this font
    /// </summary>
    public float FontSize;

    /// <summary>
    /// The current font of this snippet
    /// </summary>
    public readonly DynamicSpriteFont Font;

    /// <summary>
    /// The size of this snippet
    /// </summary>
    public Vector2 Dimensions;

    /// <summary>
    /// A unit vector describing the rotational pivot
    /// </summary>
    public readonly Vector2 Origin;

    /// <summary>
    /// The total duration of this snippet
    /// </summary>
    public readonly float Duration => Content.Length * CharacterAppearDelay;

    /// <summary>
    /// Mainly used to get the actual snippets id before word wrap modifies things
    /// </summary>
    public int OriginalID;

    #endregion

    #region Constructors

    public TextSnippet(
        string text,
        float size,
        Color? color = null,
        CharacterAppearDelegate? appear = null,
        CharacterDisplacementDelegate? displace = null,
        Vector2? origin = null,
        float characterDelay = DefaultCharacterDelay,
        DynamicSpriteFont? font = null)
    {
        Content = text;
        FontSize = size;
        TextColor = delegate { return color ?? Color.White; };
        TextAppear = appear ?? AppearSuddenly;
        TextDisplacement = displace ?? NoDisplacement;
        Origin = origin ?? Vector2.Zero;
        CharacterAppearDelay = characterDelay;
        Font = font ?? DefaultFont;
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public TextSnippet(
        string text,
        Color? color = null,
        float characterDelay = .025f,
        CharacterAppearDelegate textAppear = null,
        CharacterDisplacementDelegate textDisplacement = null,
        Vector2? origin = null,
        float fontSize = 1f,
        DynamicSpriteFont font = null)
    {
        Content = text;
        TextColor = delegate { return color ?? Color.White; };
        CharacterAppearDelay = characterDelay;
        TextAppear = textAppear ?? AppearSuddenly;
        TextDisplacement = textDisplacement ?? NoDisplacement;
        Origin = origin ?? Vector2.Zero;
        FontSize = fontSize;
        Font = font ?? FontAssets.MouseText.Value;
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public TextSnippet(
        string text,
        float size,
        LetterColorDelegate color,
        CharacterAppearDelegate? appear = null,
        CharacterDisplacementDelegate? displace = null,
        Vector2? origin = null,
        float characterDelay = DefaultCharacterDelay,
        DynamicSpriteFont? font = null)
    {
        Content = text;
        FontSize = size;
        TextColor = color;
        TextAppear = appear ?? AppearSuddenly;
        TextDisplacement = displace ?? NoDisplacement;
        Origin = origin ?? Vector2.Zero;
        CharacterAppearDelay = characterDelay;
        Font = font ?? DefaultFont;
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public TextSnippet(
        string text,
        in TextSnippet copyFrom)
    {
        Content = text;
        FontSize = copyFrom.FontSize;
        TextColor = copyFrom.TextColor;
        TextAppear = copyFrom.TextAppear;
        TextDisplacement = copyFrom.TextDisplacement;
        Origin = copyFrom.Origin;
        CharacterAppearDelay = copyFrom.CharacterAppearDelay;
        Font = copyFrom.Font;
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    #endregion

    public void DrawLetterByLetterSnippet(Vector2 position, float completion, int character,
        float opacity = 1f, float rotation = 0f)
    {
        for (int i = 0; i < Content.Length; i++)
        {
            if (completion < i * CharacterAppearDelay)
                return;

            Vector2 displacement = TextDisplacement(character + i);
            if (completion >= i * CharacterAppearDelay && completion < (i + 1) * CharacterAppearDelay &&
                TextAppear != AppearSuddenly)
                displacement += TextAppear(character + i,
                    (completion - i * CharacterAppearDelay) / CharacterAppearDelay);

            displacement = displacement.RotatedBy(rotation);

            Color mainColor = TextColor(character + i, completion) * opacity;
            DrawBorderStringEightWay(Main.spriteBatch, Font, Content[i].ToString(), position + displacement, mainColor,
                Color.Black * opacity, rotation, FontSize);
            position += Vector2.UnitX.RotatedBy(rotation) *
                        ChatManager.GetStringSize(Font, Content[i].ToString(), Vector2.One).X * FontSize;
        }
    }

    public void RecalculateDimensions()
    {
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }
}

/// <summary>
/// Defines an awesome laid-out, renderable block of text composed of <see cref="TextSnippet"/>'s
/// </summary>
public struct TextBlock
{
    #region Public Fields

    public readonly List<TextSnippet> Snippets;

    /// <summary>
    /// A [0, 1] pivot applied to the full block's bounding size, offsetting the draw origin
    /// </summary>
    public Vector2 Origin;

    public float TotalWidth { get; private set; }
    public float TotalHeight { get; private set; }

    internal float MaxProgress;
    internal float CurrentProgress;

    public float AnimationCompletion
    {
        readonly get => MaxProgress > 0 ? CurrentProgress / MaxProgress : 1f;
        set => CurrentProgress = value * MaxProgress;
    }

    #endregion

    #region Constructors

    public TextBlock(TextSnippet[] textSnippets, Vector2? origin = null)
    {
        Snippets = [];
        for (int i = 0; i < textSnippets.Length; i++)
        {
            ref TextSnippet snippet = ref textSnippets[i];
            snippet.OriginalID = i;
            Snippets.Add(snippet);
        }

        ResizeProperties();

        Origin = origin ?? Vector2.Zero;

        CurrentProgress = float.MaxValue;
    }

    #endregion

    #region Layout

    /// <summary>
    /// Wraps text at word boundaries to fit within <paramref name="textboxWidth"/>, respecting existing "\n" snippets.
    /// </summary>
    public void ApplyWordWrap(float textboxWidth)
    {
        List<TextSnippet> wrapped = [];
        float lineWidth = 0f;
        List<TextSnippet> lineSnippets = [];

        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
            {
                wrapped.AddRange(lineSnippets);
                wrapped.Add(snippet);
                lineSnippets.Clear();
                lineWidth = 0f;
                continue;
            }

            string[] words = snippet.Content.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (i < words.Length - 1)
                    word += " ";

                TextSnippet wordSnippet = new(word, snippet) { OriginalID = snippet.OriginalID };
                float wordWidth = wordSnippet.Dimensions.X;

                if (lineWidth + wordWidth > textboxWidth && lineSnippets.Count > 0)
                {
                    wrapped.AddRange(lineSnippets);
                    wrapped.Add(new TextSnippet("\n", snippet));
                    lineSnippets.Clear();
                    lineWidth = 0f;
                }

                lineSnippets.Add(wordSnippet);
                lineWidth += wordWidth;
            }
        }

        wrapped.AddRange(lineSnippets);

        Snippets.Clear();
        Snippets.AddRange(wrapped);
        ResizeProperties();
    }

    public void ResizeProperties()
    {
        TotalWidth = 0f;
        MaxProgress = 0f;

        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
                continue;

            MaxProgress += snippet.Duration;
            TotalWidth += snippet.Dimensions.X;
        }

        TotalHeight = GetTotalHeight();
    }

    #endregion

    #region Queries

    public readonly int GetTotalCharacterCount()
    {
        int count = 0;
        foreach (TextSnippet snippet in Snippets)
            count += snippet.Content.Length;
        return count;
    }

    public readonly bool GetCurrentSnippet(float progression, out int index, out TextSnippet? current)
    {
        float progress = 0f;
        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
                continue;

            float duration = snippet.Duration;
            if (progression >= progress && progression < progress + duration)
            {
                index = snippet.OriginalID;
                current = snippet;
                return true;
            }

            progress += duration;
        }

        index = -1;
        current = null;
        return false;
    }

    public readonly bool IsSnippetActive(int index, float progression)
    {
        float progress = 0f;
        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
                continue;

            float duration = snippet.Duration;
            if (snippet.OriginalID == index)
                return progression >= progress && progression < progress + duration;
            progress += duration;
        }

        return false;
    }

    public readonly float GetLineHeight(int line)
    {
        float maxHeight = 0f;
        int currentLine = 0;

        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
            {
                if (currentLine == line)
                    break;
                currentLine++;
            }
            else if (currentLine == line)
                maxHeight = Math.Max(maxHeight, snippet.Dimensions.Y);
        }

        return maxHeight;
    }

    public readonly float GetTotalHeight()
    {
        float height = GetLineHeight(0);
        int currentLine = 0;

        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content != "\n")
                continue;

            currentLine++;
            height += GetLineHeight(currentLine) * 0.6f;
        }

        return height;
    }

    public readonly string GetAllText()
    {
        StringBuilder builder = new();
        foreach (TextSnippet snippet in Snippets)
            builder.Append(snippet.Content);
        return builder.ToString();
    }

    #endregion

    #region Drawing

    public readonly void Draw(Vector2 position, float opacity = 1f, float rotation = 0f)
    {
        Vector2 originOffset = new Vector2(TotalWidth, TotalHeight) * Origin;
        Vector2 blockStart = position - originOffset.RotatedBy(rotation);

        Vector2 currentPosition = blockStart;
        Vector2 lineStart = blockStart;
        float sentenceProgress = 0f;
        int currentCharacter = 0;
        float currentLineHeight = GetLineHeight(0);
        int currentLine = 0;

        foreach (TextSnippet snippet in Snippets)
        {
            if (sentenceProgress > CurrentProgress)
                break;

            if (snippet.Content == "\n")
            {
                lineStart += Vector2.UnitY.RotatedBy(rotation) * currentLineHeight * 0.75f;
                currentPosition = lineStart;
                currentLine++;
                currentLineHeight = GetLineHeight(currentLine);
                continue;
            }

            Vector2 verticalAlign = Vector2.UnitY * (currentLineHeight - snippet.Dimensions.Y) / 2f;
            snippet.DrawLetterByLetterSnippet(currentPosition + verticalAlign, CurrentProgress - sentenceProgress,
                currentCharacter, opacity, rotation);

            currentPosition += snippet.Dimensions.X * Vector2.UnitX.RotatedBy(rotation);
            sentenceProgress += snippet.Duration;
            currentCharacter += snippet.Content.Length;
        }
    }

    #endregion
}

public sealed class DialogueManager(
    Vector2 position,
    float delayBetweenSentences = 0.5f,
    float fadeRatio = 0f,
    float rotation = 0f)
{
    private readonly Queue<TextBlock> Sentences = new();
    public TextBlock? CurrentSentence;
    public float CurrentProgress;
    private float TimeSinceSentenceEnd;
    public Vector2 Position = position;
    public float Rotation = rotation;
    public float FadeRatio = fadeRatio;
    public bool Active;

    public bool IsComplete => !Active && Sentences.Count == 0 && CurrentSentence == null;

    public void AddBlock(in TextBlock sentence) => Sentences.Enqueue(sentence);

    public void AddBlock(in TextBlock[] newSentences)
    {
        foreach (TextBlock sentence in newSentences)
            Sentences.Enqueue(sentence);
    }

    public void Start()
    {
        Active = true;
        if (CurrentSentence == null && Sentences.Count > 0)
        {
            CurrentSentence = Sentences.Dequeue();
            CurrentProgress = 0f;
            TimeSinceSentenceEnd = 0f;
        }
    }

    public void Update(float progressionIncrement)
    {
        if (!Active || Main.dedServ)
            return;

        if (CurrentSentence == null)
        {
            if (Sentences.Count > 0)
            {
                CurrentSentence = Sentences.Dequeue();
                CurrentProgress = 0f;
                TimeSinceSentenceEnd = 0f;
            }
            else
                Active = false;

            return;
        }

        if (CurrentProgress < CurrentSentence.Value.MaxProgress)
            CurrentProgress += progressionIncrement;
        else
        {
            TimeSinceSentenceEnd += progressionIncrement;
            if (TimeSinceSentenceEnd >= delayBetweenSentences)
            {
                CurrentSentence = null; // Move to the next sentence
                TimeSinceSentenceEnd = 0f;
            }
        }
    }

    public void Draw()
    {
        if (!Active || Main.dedServ || CurrentSentence == null)
            return;

        float opacity = 1f;
        if (CurrentProgress >= CurrentSentence.Value.MaxProgress && FadeRatio > 0)
        {
            float fadeDuration = delayBetweenSentences * FadeRatio;
            opacity = InverseLerp(delayBetweenSentences, delayBetweenSentences - fadeDuration, TimeSinceSentenceEnd);
        }

        CurrentSentence.Value.Draw(Position, opacity);
    }

    public void SkipCurrentSentence()
    {
        if (CurrentSentence != null)
        {
            // Skip to the end of the current sentence, triggering the delay
            CurrentProgress = CurrentSentence.Value.MaxProgress;
            TimeSinceSentenceEnd = 0f;
        }
    }

    /// <summary>
    /// Clears all sentences and resets the manager
    /// </summary>
    public void Clear()
    {
        Sentences.Clear();
        CurrentSentence = null;
        CurrentProgress = 0f;
        TimeSinceSentenceEnd = 0f;
        Active = false;
    }
}
