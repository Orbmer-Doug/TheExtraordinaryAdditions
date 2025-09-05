using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Core.Systems;

// TODO: Shaders? Opacity delegate?
public delegate Vector2 CharacterDisplacementDelegate(int character);
public delegate Vector2 CharacterAppearDelegate(int character, float progress);
public delegate Color LetterColorDelegate(int character, float globalProgress);

/// <summary>
/// An individual segment of text that can have a variety of different effects applied to it, but the effects are all consistent throughout the text displayed <br/>
/// Multiple of them can be chained after one another into a coherent <see cref="AwesomeSentence"/>
/// </summary>
public struct TextSnippet
{
    public static Vector2 NoDisplacement(int character) => Vector2.Zero;
    public static Vector2 SmallRandomDisplacement(int character) => Main.rand.NextVector2Circular(1.1f, 1.1f);
    public static Vector2 RandomDisplacement(int character) => Main.rand.NextVector2Circular(2f, 2f);
    public static Vector2 SmallWaveDisplacement(int character) => new Vector2(0, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f + character * 0.8f) * 2.5f);
    public static Vector2 WaveDisplacement(int character) => new Vector2(0, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f + character * 0.8f) * 4f);
    public static Vector2 WaveEmphasisDisplacement(int character) => new Vector2(0, -4f * MathHelper.Clamp(((float)Math.Sin(-Main.GlobalTimeWrappedHourly * 4f + character * 0.2f) - 0.7f) / 0.3f, 0f, 1f));

    public static Vector2 AppearSuddenly(int character, float progress) => Vector2.Zero;
    public static Vector2 AppearFadingFromTop(int character, float progress) => new Vector2(0, -Animators.MakePoly(1.6f).InFunction(1 - progress) * 16f);
    public static Vector2 AppearFadingFromTopZipper(int character, float progress) => new Vector2(0, -Animators.MakePoly(1.6f).InFunction(1 - progress) * 16f * (character % 2 == 1 ? 1 : -1));
    public static Vector2 AppearFadingFromRight(int character, float progress) => new Vector2(Animators.MakePoly(2.1f).OutFunction(1 - progress) * 16f, 0f);

    public string Content;
    public LetterColorDelegate TextColor;
    public float CharacterAppearDelay;
    public CharacterAppearDelegate TextAppear;
    public CharacterDisplacementDelegate TextDisplacement;
    public bool NewLine;
    public float FontSize;
    public readonly DynamicSpriteFont Font;
    public Vector2 Dimensions;
    public float Duration => Content.Length * CharacterAppearDelay;

    /// <summary>
    /// Used mainly by <see cref="AwesomeSentence"/> <br></br>
    /// Helps with retaining the original index within <see cref="AwesomeSentence.Snippets"/> before <see cref="AwesomeSentence.ReWrap(float)"/> modifies it
    /// </summary>
    public int OriginalID;

    public TextSnippet(string text, Color? color = null, float characterDelay = .025f, CharacterAppearDelegate textAppear = null, CharacterDisplacementDelegate textDisplacement = null,
        bool newLine = false, float fontSize = 1f, DynamicSpriteFont font = null)
    {
        if (Main.dedServ)
        {
            Content = "";
            TextColor = default;
            CharacterAppearDelay = default;
            TextAppear = default;
            TextDisplacement = default;
            NewLine = default;
            FontSize = default;
            Font = default;
            Dimensions = default;
            return;
        }

        Content = text;
        TextColor = delegate (int character, float globalProgress) { return color ?? Color.White; };
        CharacterAppearDelay = characterDelay;
        TextAppear = textAppear ?? AppearSuddenly;
        TextDisplacement = textDisplacement ?? NoDisplacement;
        NewLine = newLine;
        FontSize = fontSize;
        Font = font ?? FontAssets.MouseText.Value;

        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public TextSnippet(string text, LetterColorDelegate color, float characterDelay = .025f, CharacterAppearDelegate textAppear = null, CharacterDisplacementDelegate textDisplacement = null,
        bool newLine = false, float fontSize = 1f, DynamicSpriteFont font = null)
    {
        if (Main.dedServ)
        {
            Content = "";
            TextColor = default;
            CharacterAppearDelay = default;
            TextAppear = default;
            TextDisplacement = default;
            NewLine = default;
            FontSize = default;
            Font = default;
            Dimensions = default;
            return;
        }

        Content = text;
        TextColor = color;
        CharacterAppearDelay = characterDelay;
        TextAppear = textAppear ?? AppearSuddenly;
        TextDisplacement = textDisplacement ?? NoDisplacement;
        NewLine = newLine;
        FontSize = fontSize;
        Font = font ?? FontAssets.MouseText.Value;

        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public TextSnippet(string text, TextSnippet copyFrom)
    {
        if (Main.dedServ)
        {
            Content = "";
            TextColor = default;
            CharacterAppearDelay = default;
            TextAppear = default;
            TextDisplacement = default;
            NewLine = default;
            FontSize = default;
            Font = default;
            Dimensions = default;
            return;
        }

        Content = text;
        TextColor = copyFrom.TextColor;
        CharacterAppearDelay = copyFrom.CharacterAppearDelay;
        TextAppear = copyFrom.TextAppear;
        TextDisplacement = copyFrom.TextDisplacement;
        NewLine = copyFrom.NewLine;
        FontSize = copyFrom.FontSize;
        Font = copyFrom.Font;

        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }

    public void DrawLetterByLetterSnippet(SpriteBatch sb, Vector2 position, float completion, int character, float opacity = 1f, float rotation = 0f)
    {
        for (int i = 0; i < Content.Length; i++)
        {
            if (completion < i * CharacterAppearDelay)
                return;

            Vector2 displacement = TextDisplacement(character + i);
            if (completion >= i * CharacterAppearDelay && completion < (i + 1) * CharacterAppearDelay && TextAppear != AppearSuddenly)
                displacement += TextAppear(character + i, (completion - i * CharacterAppearDelay) / CharacterAppearDelay);

            displacement = displacement.RotatedBy(rotation);

            Color mainColor = TextColor(character + i, completion) * opacity;
            DrawBorderStringEightWay(sb, Font, Content[i].ToString(), position + displacement, mainColor, Color.Black * opacity, rotation, FontSize);
            position += Vector2.UnitX.RotatedBy(rotation) * ChatManager.GetStringSize(Font, Content[i].ToString(), Vector2.One).X * FontSize;
        }
    }

    public void RecalculateDimensions()
    {
        Dimensions = ChatManager.GetStringSize(Font, Content, Vector2.One) * FontSize;
    }
}

/// <summary>
/// Text that can be scrolled one letter at a time, made up of multiple different <see cref="TextSnippet"/>s, allowing the text to have pauses, different font sizes, moving text, etc.
/// </summary>
// ...paragraph?
public class AwesomeSentence
{
    public List<TextSnippet> Snippets;
    public float MaxProgress;
    public float TotalWidth;

    public AwesomeSentence(float textboxWidth, params TextSnippet[] textSnippets)
    {
        if (Main.dedServ)
            return;

        Snippets = new List<TextSnippet>();
        for (int i = 0; i < textSnippets.Length; i++)
        {
            TextSnippet snippet = textSnippets[i];
            snippet.OriginalID = i;
            Snippets.Add(snippet);
        }

        ResizeProperties();

        if (textboxWidth >= TotalWidth)
            return;
        else
            ReWrap(textboxWidth);
    }

    public void ReWrap(float newWidth)
    {
        // Remove only automatic line breaks (keep manual ones)
        Snippets.RemoveAll(t => t.Content == "\n" && !t.NewLine);

        float lineWidth = 0f;
        List<TextSnippet> wrappedSnippets = new List<TextSnippet>();
        int i = 0;

        while (i < Snippets.Count)
        {
            TextSnippet currentSnippet = Snippets[i];
            if (currentSnippet.Content == "\n") // Handle explicit \n snippets
            {
                wrappedSnippets.Add(currentSnippet);
                lineWidth = 0;
                i++;
                continue;
            }

            float upcomingSnippetWidth = currentSnippet.Dimensions.X;

            // If the snippet is marked as NewLine, add a line break
            if (currentSnippet.NewLine && lineWidth > 0)
            {
                wrappedSnippets.Add(new TextSnippet("\n") { NewLine = true, OriginalID = -1 });
                lineWidth = 0;
            }

            // If the snippet fits on the current line
            if (lineWidth + upcomingSnippetWidth <= newWidth || currentSnippet.NewLine)
            {
                lineWidth += upcomingSnippetWidth;
                wrappedSnippets.Add(currentSnippet);
                i++;

                // If we perfectly matched the edge of the textbox, add a line break
                if (lineWidth == newWidth && i < Snippets.Count && !Snippets[i].NewLine)
                {
                    wrappedSnippets.Add(new TextSnippet("\n") { OriginalID = -1 });
                    lineWidth = 0;
                }
            }
            else
            {
                // Split the snippet if it doesn't fit
                string splitSnippetRightHalf = currentSnippet.Content;
                string splitSnippetLeftHalf = "";
                MatchCollection firstWordRegex = Regex.Matches(splitSnippetRightHalf, "\\s*\\S+");

                bool addedFragment = false;
                while (firstWordRegex.Count > 0)
                {
                    string nextSnippetFragment = firstWordRegex[0].Value;
                    float nextSnippetFragmentWidth = ChatManager.GetStringSize(currentSnippet.Font, nextSnippetFragment, Vector2.One).X * currentSnippet.FontSize;

                    if (lineWidth + nextSnippetFragmentWidth <= newWidth)
                    {
                        lineWidth += nextSnippetFragmentWidth;
                        splitSnippetLeftHalf += nextSnippetFragment;
                        splitSnippetRightHalf = splitSnippetRightHalf.Substring(nextSnippetFragment.Length);
                        firstWordRegex = Regex.Matches(splitSnippetRightHalf, "\\s*\\S+");

                        if (firstWordRegex.Count == 0 && splitSnippetRightHalf.Length > 0)
                        {
                            lineWidth += ChatManager.GetStringSize(currentSnippet.Font, splitSnippetRightHalf, Vector2.One).X * currentSnippet.FontSize;
                            wrappedSnippets.Add(currentSnippet);
                            i++;
                            addedFragment = true;
                        }
                    }
                    else
                    {
                        if (splitSnippetLeftHalf != "")
                        {
                            TextSnippet leftSnippet = new TextSnippet(splitSnippetLeftHalf, currentSnippet);
                            leftSnippet.OriginalID = currentSnippet.OriginalID;
                            wrappedSnippets.Add(leftSnippet);
                        }
                        wrappedSnippets.Add(new TextSnippet("\n"));
                        lineWidth = 0;
                        TextSnippet rightSnippet = new TextSnippet(splitSnippetRightHalf, currentSnippet);
                        rightSnippet.OriginalID = currentSnippet.OriginalID;
                        Snippets[i] = rightSnippet;
                        addedFragment = true;
                        break;
                    }
                }

                if (!addedFragment)
                {
                    wrappedSnippets.Add(currentSnippet);
                    i++;
                }
            }
        }

        Snippets = wrappedSnippets;
        ResizeProperties();
    }

    public void ResizeProperties()
    {
        List<TextSnippet> linebreakLessSnippets = Snippets.FindAll(t => t.Content != "\n");

        MaxProgress = 0f;
        TotalWidth = 0f;
        foreach (TextSnippet snippet in linebreakLessSnippets)
        {
            MaxProgress += snippet.Duration;
            TotalWidth += snippet.Dimensions.X;
        }
    }

    public int GetTimeToSnippet(int index)
    {
        if (index < 0)
            return 0;

        float totalSeconds = 0f;
        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
                continue;
            if (snippet.OriginalID == index)
                break;

            if (snippet.OriginalID >= 0 && snippet.OriginalID < index)
                totalSeconds += snippet.Duration;
        }

        return (int)(totalSeconds * 60f);
    }

    public bool GetCurrentSnippet(float progression, out int index, out TextSnippet? current)
    {
        float currentProgress = 0f;
        for (int i = 0; i < Snippets.Count; i++)
        {
            TextSnippet snippet = Snippets[i];
            if (snippet.Content == "\n")
                continue;

            float snippetDuration = snippet.Duration;
            if (progression >= currentProgress && progression < currentProgress + snippetDuration)
            {
                index = snippet.OriginalID;
                current = snippet;
                return true;
            }
            currentProgress += snippetDuration;
        }
        index = -1;
        current = null;
        return false;
    }

    public bool IsSnippetActive(int index, float progression)
    {
        float currentProgress = 0f;
        for (int i = 0; i < Snippets.Count; i++)
        {
            TextSnippet snippet = Snippets[i];
            if (snippet.Content == "\n")
                continue;

            float snippetDuration = snippet.Duration;
            if (snippet.OriginalID == index)
                return progression >= currentProgress && progression < currentProgress + snippetDuration;
            currentProgress += snippetDuration;
        }
        return false;
    }

    public float GetLineHeight(int line)
    {
        float maxLineHeight = 0f;
        int currentLine = 0;

        for (int i = 0; i < Snippets.Count; i++)
        {
            if (Snippets[i].Content == "\n")
            {
                if (currentLine == line)
                    break;
                currentLine++;
            }
            else if (currentLine == line)
                maxLineHeight = Math.Max(maxLineHeight, Snippets[i].Dimensions.Y);
        }

        return maxLineHeight;
    }

    public float GetTotalHeight()
    {
        float height = GetLineHeight(0);
        int currentLine = 0;

        foreach (TextSnippet snippet in Snippets)
        {
            if (snippet.Content == "\n")
            {
                currentLine++;
                height += GetLineHeight(currentLine) * 0.6f;
            }
        }

        return height;
    }

    public void Draw(float progression, Vector2 position, float opacity = 1f, float rotation = 0f)
    {
        Vector2 currentPosition = position;
        Vector2 currentTextBorder = position;
        float sentenceProgress = 0f;
        int currentCharacter = 0;
        float currentLineHeight = GetLineHeight(0);
        int currentLine = 0;

        foreach (TextSnippet snippet in Snippets)
        {
            if (sentenceProgress > progression)
                return;

            if (snippet.Content == "\n")
            {
                currentTextBorder += Vector2.UnitY.RotatedBy(rotation) * currentLineHeight * 0.6f;
                currentPosition = currentTextBorder;
                currentLine++;
                currentLineHeight = GetLineHeight(currentLine);
                continue;
            }

            Vector2 snippetHeightDown = Vector2.UnitY * (currentLineHeight - snippet.Dimensions.Y) / 2f;
            snippet.DrawLetterByLetterSnippet(Main.spriteBatch, currentPosition + snippetHeightDown, progression - sentenceProgress, currentCharacter, opacity, rotation);
            currentPosition += snippet.Dimensions.X * Vector2.UnitX.RotatedBy(rotation);

            sentenceProgress += snippet.Duration;
            currentCharacter += snippet.Content.Length;
        }
    }
}

public class DialogueManager
{
    private Queue<AwesomeSentence> Sentences;
    public AwesomeSentence CurrentSentence;
    public float CurrentProgress;
    private float TimeSinceSentenceEnd;
    private float DelayBetweenSentences;
    public Vector2 Position;
    public float Rotation;
    public float FadeRatio;
    public bool Active;

    public bool IsComplete => !Active && Sentences.Count == 0 && CurrentSentence == null;

    public DialogueManager(Vector2 position, float delayBetweenSentences = 0.5f, float fadeRatio = 0f, float rotation = 0f)
    {
        Sentences = new Queue<AwesomeSentence>();
        Position = position;
        Rotation = rotation;
        DelayBetweenSentences = delayBetweenSentences;
        FadeRatio = fadeRatio;
        CurrentProgress = 0f;
        TimeSinceSentenceEnd = 0f;
        Active = false;
        CurrentSentence = null;
    }

    public void AddSentence(in AwesomeSentence sentence) => Sentences.Enqueue(sentence);

    public void AddSentences(in AwesomeSentence[] newSentences)
    {
        foreach (AwesomeSentence sentence in newSentences)
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

        if (CurrentProgress < CurrentSentence.MaxProgress)
            CurrentProgress += progressionIncrement;
        else
        {
            TimeSinceSentenceEnd += progressionIncrement;
            if (TimeSinceSentenceEnd >= DelayBetweenSentences)
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
        if (CurrentProgress >= CurrentSentence.MaxProgress && FadeRatio > 0)
        {
            float fadeDuration = DelayBetweenSentences * FadeRatio;
            opacity = InverseLerp(DelayBetweenSentences, DelayBetweenSentences - fadeDuration, TimeSinceSentenceEnd);
        }

        CurrentSentence.Draw(CurrentProgress, Position, opacity, Rotation);
    }

    public void SkipCurrentSentence()
    {
        if (CurrentSentence != null)
        {
            // Skip to the end of the current sentence, triggering the delay
            CurrentProgress = CurrentSentence.MaxProgress;
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