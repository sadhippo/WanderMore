using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace HiddenHorizons;

public class EventCardBox
{
    // UI Properties
    public bool IsVisible { get; private set; }
    public Rectangle Bounds { get; set; }
    public SpriteFont Font { get; set; }
    
    // Layout Areas
    public Rectangle ImageArea { get; private set; }
    public Rectangle TextArea { get; private set; }
    
    // Dialogue State
    public DialogueNode CurrentNode { get; private set; }
    public Texture2D EventImage { get; private set; }
    public List<DialogueChoice> CurrentChoices { get; private set; }
    
    // Timing and Animation
    public float AutoTimeoutSeconds { get; set; } = 60f;
    public float CurrentTimeout { get; private set; }
    private bool _timeoutOccurred = false;
    private float _timeoutFeedbackTimer = 0f;
    private const float TIMEOUT_FEEDBACK_DURATION = 2f;
    
    // Visual Polish and Animation Properties
    private float _fadeAlpha = 0f;
    private float _cardScale = 0.8f;
    private bool _isAnimatingIn = false;
    private bool _isAnimatingOut = false;
    private const float FADE_SPEED = 4f;
    private const float SCALE_SPEED = 6f;
    private const float MIN_SCALE = 0.8f;
    private const float MAX_SCALE = 1f;
    
    // Button Animation Properties
    private Dictionary<int, float> _buttonScales = new Dictionary<int, float>();
    private Dictionary<int, float> _buttonGlowIntensity = new Dictionary<int, float>();
    private int _pressedButtonIndex = -1;
    private float _buttonPressTimer = 0f;
    private const float BUTTON_PRESS_DURATION = 0.15f;
    private const float BUTTON_HOVER_SCALE = 1.05f;
    private const float BUTTON_PRESS_SCALE = 0.95f;
    
    // Background Dimming Animation
    private float _backgroundDimAlpha = 0f;
    private const float BACKGROUND_DIM_SPEED = 3f;
    private const float MAX_BACKGROUND_DIM = 0.8f;
    
    // Additional Visual Polish
    private float _pulseTimer = 0f;
    private const float PULSE_SPEED = 2f;
    private float _cardShadowOffset = 8f;
    
    // Events
    public event Action<DialogueChoice> ChoiceSelected;
    public event Action DialogueClosed;
    
    // Rendering resources
    private Texture2D _pixelTexture;
    private GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;
    private int _hoveredChoiceIndex = -1;
    
    // Choice button areas
    private List<Rectangle> _choiceButtonAreas;
    
    // Layout constants
    private const int IMAGE_SIZE = 200;
    private const int MARGIN = 20;
    private const int CHOICE_BUTTON_HEIGHT = 40;
    private const int CHOICE_BUTTON_SPACING = 10;

    public EventCardBox(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        CurrentChoices = new List<DialogueChoice>();
        _choiceButtonAreas = new List<Rectangle>();
        
        // Create pixel texture for drawing backgrounds and borders
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        
        IsVisible = false;
    }

    public void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        CalculateLayout(screenWidth, screenHeight);
    }

    private void CalculateLayout(int screenWidth, int screenHeight)
    {
        // Calculate card bounds (80% screen width, 60% screen height, centered)
        int cardWidth = (int)(screenWidth * 0.8f);
        int cardHeight = (int)(screenHeight * 0.6f);
        
        Bounds = new Rectangle(
            (screenWidth - cardWidth) / 2,
            (screenHeight - cardHeight) / 2,
            cardWidth,
            cardHeight
        );
        
        // Calculate image area (200x200px on the left)
        ImageArea = new Rectangle(
            Bounds.X + MARGIN,
            Bounds.Y + MARGIN + 30, // Leave space for title
            IMAGE_SIZE,
            IMAGE_SIZE
        );
        
        // Calculate text area (remaining width for text and buttons)
        TextArea = new Rectangle(
            ImageArea.Right + MARGIN,
            ImageArea.Y,
            Bounds.Width - IMAGE_SIZE - (MARGIN * 3),
            Bounds.Height - MARGIN - 30 // Account for title space
        );
        
        // Recalculate choice button areas if we have choices
        if (CurrentChoices != null && CurrentChoices.Count > 0)
        {
            CalculateChoiceButtonAreas();
        }
    }

    private void CalculateChoiceButtonAreas()
    {
        _choiceButtonAreas.Clear();
        
        if (CurrentChoices == null || CurrentChoices.Count == 0)
            return;
        
        // Calculate button area within text area (bottom portion)
        int buttonAreaHeight = (CurrentChoices.Count * CHOICE_BUTTON_HEIGHT) + 
                              ((CurrentChoices.Count - 1) * CHOICE_BUTTON_SPACING);
        
        int buttonStartY = TextArea.Bottom - buttonAreaHeight - MARGIN;
        
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            Rectangle buttonArea = new Rectangle(
                TextArea.X,
                buttonStartY + (i * (CHOICE_BUTTON_HEIGHT + CHOICE_BUTTON_SPACING)),
                TextArea.Width,
                CHOICE_BUTTON_HEIGHT
            );
            
            _choiceButtonAreas.Add(buttonArea);
        }
    }

    public void ShowDialogue(DialogueNode startNode)
    {
        CurrentNode = startNode;
        CurrentChoices = startNode?.Choices ?? new List<DialogueChoice>();
        CurrentTimeout = AutoTimeoutSeconds;
        _timeoutOccurred = false;
        _timeoutFeedbackTimer = 0f;
        IsVisible = true;
        
        // Initialize animation state for fade-in
        _fadeAlpha = 0f;
        _cardScale = MIN_SCALE;
        _backgroundDimAlpha = 0f;
        _isAnimatingIn = true;
        _isAnimatingOut = false;
        
        // Initialize button animation states
        _buttonScales.Clear();
        _buttonGlowIntensity.Clear();
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            _buttonScales[i] = 1f;
            _buttonGlowIntensity[i] = 0f;
        }
        
        // Recalculate layout for new dialogue
        CalculateLayout(_screenWidth, _screenHeight);
        CalculateChoiceButtonAreas();
        
        System.Console.WriteLine($"[EventCardBox] Showing dialogue: {startNode?.QuestText}");
        System.Console.WriteLine($"[EventCardBox] Choices: {CurrentChoices.Count}");
    }

    public void UpdateDialogue(DialogueNode newNode)
    {
        CurrentNode = newNode;
        CurrentChoices = newNode?.Choices ?? new List<DialogueChoice>();
        CurrentTimeout = AutoTimeoutSeconds; // Reset timeout for new dialogue
        _timeoutOccurred = false;
        _timeoutFeedbackTimer = 0f;
        
        // Reset button animation states for new choices
        _buttonScales.Clear();
        _buttonGlowIntensity.Clear();
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            _buttonScales[i] = 1f;
            _buttonGlowIntensity[i] = 0f;
        }
        _hoveredChoiceIndex = -1;
        
        // Recalculate choice button areas for new choices
        CalculateChoiceButtonAreas();
        
        System.Console.WriteLine($"[EventCardBox] Updated dialogue: {newNode?.QuestText}");
        System.Console.WriteLine($"[EventCardBox] New choices: {CurrentChoices.Count}");
    }

    public void SetEventImage(Texture2D eventImage)
    {
        EventImage = eventImage;
        System.Console.WriteLine($"[EventCardBox] Event image set: {eventImage != null}");
    }

    public void HandleChoiceSelection(int choiceIndex, bool isAutoTimeout = false)
    {
        if (choiceIndex < 0 || choiceIndex >= CurrentChoices.Count)
            return;
        
        var selectedChoice = CurrentChoices[choiceIndex];
        
        if (isAutoTimeout)
        {
            System.Console.WriteLine($"[EventCardBox] Auto-timeout selected choice: {selectedChoice.Text}");
            _timeoutOccurred = true;
            _timeoutFeedbackTimer = TIMEOUT_FEEDBACK_DURATION;
        }
        else
        {
            System.Console.WriteLine($"[EventCardBox] User selected choice: {selectedChoice.Text}");
            // Cancel timeout when user interacts
            CurrentTimeout = 0f;
            _timeoutOccurred = false;
            _timeoutFeedbackTimer = 0f;
            
            // Trigger button press animation
            _pressedButtonIndex = choiceIndex;
            _buttonPressTimer = BUTTON_PRESS_DURATION;
        }
        
        // Trigger choice selected event
        ChoiceSelected?.Invoke(selectedChoice);
        
        // If this choice ends the dialogue, close the card
        if (string.IsNullOrEmpty(selectedChoice.NextNodeId))
        {
            Hide();
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!IsVisible)
            return;
        
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update fade-in/out animations
        UpdateFadeAnimations(deltaTime);
        
        // Update button animations
        UpdateButtonAnimations(deltaTime);
        
        // Update timeout feedback timer
        if (_timeoutFeedbackTimer > 0f)
        {
            _timeoutFeedbackTimer -= deltaTime;
        }
        
        // Update timeout
        if (CurrentTimeout > 0f)
        {
            CurrentTimeout -= deltaTime;
            
            // Auto-select random choice when timeout expires
            if (CurrentTimeout <= 0f && CurrentChoices.Count > 0)
            {
                var random = new Random();
                int randomChoice = random.Next(CurrentChoices.Count);
                System.Console.WriteLine($"[EventCardBox] Auto-timeout: selecting choice {randomChoice}");
                HandleChoiceSelection(randomChoice, isAutoTimeout: true);
            }
        }
        
        // Handle mouse hover for choice buttons
        var mouseState = Mouse.GetState();
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        int previousHoveredIndex = _hoveredChoiceIndex;
        _hoveredChoiceIndex = -1;
        for (int i = 0; i < _choiceButtonAreas.Count; i++)
        {
            if (_choiceButtonAreas[i].Contains(mousePos))
            {
                _hoveredChoiceIndex = i;
                break;
            }
        }
        
        // Initialize button animation states if needed
        if (previousHoveredIndex != _hoveredChoiceIndex)
        {
            // Reset previous button
            if (previousHoveredIndex >= 0 && _buttonScales.ContainsKey(previousHoveredIndex))
            {
                // Will animate back to normal in UpdateButtonAnimations
            }
            
            // Initialize new hovered button if needed
            if (_hoveredChoiceIndex >= 0 && !_buttonScales.ContainsKey(_hoveredChoiceIndex))
            {
                _buttonScales[_hoveredChoiceIndex] = 1f;
                _buttonGlowIntensity[_hoveredChoiceIndex] = 0f;
            }
        }
    }

    private void UpdateFadeAnimations(float deltaTime)
    {
        // Update fade-in animation
        if (_isAnimatingIn)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha + (FADE_SPEED * deltaTime), 0f, 1f);
            _cardScale = MathHelper.Clamp(_cardScale + (SCALE_SPEED * deltaTime), MIN_SCALE, MAX_SCALE);
            _backgroundDimAlpha = MathHelper.Clamp(_backgroundDimAlpha + (BACKGROUND_DIM_SPEED * deltaTime), 0f, MAX_BACKGROUND_DIM);
            
            if (_fadeAlpha >= 1f && _cardScale >= MAX_SCALE)
            {
                _isAnimatingIn = false;
            }
        }
        
        // Update fade-out animation
        if (_isAnimatingOut)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha - (FADE_SPEED * deltaTime), 0f, 1f);
            _cardScale = MathHelper.Clamp(_cardScale - (SCALE_SPEED * deltaTime), MIN_SCALE, MAX_SCALE);
            _backgroundDimAlpha = MathHelper.Clamp(_backgroundDimAlpha - (BACKGROUND_DIM_SPEED * deltaTime), 0f, MAX_BACKGROUND_DIM);
            
            if (_fadeAlpha <= 0f)
            {
                _isAnimatingOut = false;
                IsVisible = false;
                DialogueClosed?.Invoke();
            }
        }
    }

    private void UpdateButtonAnimations(float deltaTime)
    {
        // Update pulse timer for visual effects
        _pulseTimer += deltaTime * PULSE_SPEED;
        
        // Update button press animation
        if (_buttonPressTimer > 0f)
        {
            _buttonPressTimer -= deltaTime;
        }
        
        // Update button scales and glow effects
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            if (!_buttonScales.ContainsKey(i))
            {
                _buttonScales[i] = 1f;
                _buttonGlowIntensity[i] = 0f;
            }
            
            float targetScale = 1f;
            float targetGlow = 0f;
            
            // Determine target values based on button state
            if (_pressedButtonIndex == i && _buttonPressTimer > 0f)
            {
                // Button is being pressed
                targetScale = BUTTON_PRESS_SCALE;
                targetGlow = 1f;
            }
            else if (_hoveredChoiceIndex == i)
            {
                // Button is hovered with subtle pulse effect
                float pulseEffect = 1f + (MathF.Sin(_pulseTimer) * 0.02f);
                targetScale = BUTTON_HOVER_SCALE * pulseEffect;
                targetGlow = 0.3f + (MathF.Sin(_pulseTimer * 1.5f) * 0.1f);
            }
            
            // Animate towards target values
            float scaleSpeed = 8f;
            float glowSpeed = 6f;
            
            _buttonScales[i] = MathHelper.Lerp(_buttonScales[i], targetScale, scaleSpeed * deltaTime);
            _buttonGlowIntensity[i] = MathHelper.Lerp(_buttonGlowIntensity[i], targetGlow, glowSpeed * deltaTime);
        }
    }

    public bool HandleMouseClick(Vector2 mousePosition)
    {
        if (!IsVisible)
            return false;
        
        // Check if click is within the card bounds
        if (!Bounds.Contains(mousePosition))
            return false;
        
        // Check choice button clicks
        for (int i = 0; i < _choiceButtonAreas.Count; i++)
        {
            if (_choiceButtonAreas[i].Contains(mousePosition))
            {
                HandleChoiceSelection(i);
                return true;
            }
        }
        
        return true; // Consume click even if not on a button (prevents clicking through)
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || CurrentNode == null)
            return;
        
        // Don't draw anything if we're fully faded out
        if (_fadeAlpha <= 0f)
            return;
        
        // Draw animated background dimming overlay
        Rectangle fullScreen = new Rectangle(0, 0, _screenWidth, _screenHeight);
        spriteBatch.Draw(_pixelTexture, fullScreen, Color.Black * _backgroundDimAlpha);
        
        // Calculate scaled card bounds for animation
        Rectangle scaledBounds = GetScaledCardBounds();
        
        // Draw card drop shadow for depth
        Rectangle shadowBounds = new Rectangle(
            scaledBounds.X + (int)(_cardShadowOffset * _fadeAlpha),
            scaledBounds.Y + (int)(_cardShadowOffset * _fadeAlpha),
            scaledBounds.Width,
            scaledBounds.Height
        );
        spriteBatch.Draw(_pixelTexture, shadowBounds, Color.Black * (0.4f * _fadeAlpha));
        
        // Draw card background with fade effect
        spriteBatch.Draw(_pixelTexture, scaledBounds, Color.Black * (0.9f * _fadeAlpha));
        DrawBorder(spriteBatch, scaledBounds, Color.White * _fadeAlpha, 2);
        
        if (Font != null)
        {
            // Draw title (speaker name or event title) with fade effect
            string title = !string.IsNullOrEmpty(CurrentNode.SpeakerName) 
                ? CurrentNode.SpeakerName 
                : "Event";
            
            Vector2 titleSize = Font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                scaledBounds.X + (scaledBounds.Width - titleSize.X) / 2,
                scaledBounds.Y + 10
            );
            spriteBatch.DrawString(Font, title, titlePos, Color.Yellow * _fadeAlpha);
            
            // Calculate scaled image area
            Rectangle scaledImageArea = GetScaledImageArea(scaledBounds);
            
            // Draw event image if available with fade effect
            if (EventImage != null)
            {
                spriteBatch.Draw(EventImage, scaledImageArea, Color.White * _fadeAlpha);
                DrawBorder(spriteBatch, scaledImageArea, Color.Gray * _fadeAlpha, 1);
            }
            else
            {
                // Draw placeholder for missing image with fade effect
                spriteBatch.Draw(_pixelTexture, scaledImageArea, Color.DarkGray * (0.5f * _fadeAlpha));
                DrawBorder(spriteBatch, scaledImageArea, Color.Gray * _fadeAlpha, 1);
                
                // Draw "No Image" text with fade effect
                string noImageText = "No Image";
                Vector2 noImageSize = Font.MeasureString(noImageText);
                Vector2 noImagePos = new Vector2(
                    scaledImageArea.X + (scaledImageArea.Width - noImageSize.X) / 2,
                    scaledImageArea.Y + (scaledImageArea.Height - noImageSize.Y) / 2
                );
                spriteBatch.DrawString(Font, noImageText, noImagePos, Color.Gray * _fadeAlpha);
            }
            
            // Draw quest text (main dialogue text) with fade effect
            if (!string.IsNullOrEmpty(CurrentNode.QuestText))
            {
                // Calculate scaled text area
                Rectangle scaledTextArea = GetScaledTextArea(scaledBounds);
                
                // Calculate text area for quest text (above choice buttons)
                int textHeight = _choiceButtonAreas.Count > 0 
                    ? _choiceButtonAreas[0].Y - scaledTextArea.Y - MARGIN
                    : scaledTextArea.Height - MARGIN;
                
                Rectangle questTextArea = new Rectangle(
                    scaledTextArea.X,
                    scaledTextArea.Y,
                    scaledTextArea.Width,
                    textHeight
                );
                
                DrawWrappedText(spriteBatch, CurrentNode.QuestText, questTextArea, Color.White * _fadeAlpha);
            }
            
            // Draw choice buttons with animations
            DrawChoiceButtons(spriteBatch, scaledBounds);
            
            // Draw timeout indicator with fade effect
            if (CurrentTimeout > 0f && CurrentTimeout <= 10f) // Show when less than 10 seconds remain
            {
                DrawTimeoutIndicator(spriteBatch, scaledBounds);
            }
            
            // Draw timeout feedback with fade effect
            if (_timeoutFeedbackTimer > 0f)
            {
                DrawTimeoutFeedback(spriteBatch, scaledBounds);
            }
        }
        else
        {
            // Fallback rendering without font
            DrawFallbackContent(spriteBatch);
        }
    }

    private Rectangle GetScaledCardBounds()
    {
        Vector2 center = new Vector2(Bounds.X + Bounds.Width / 2f, Bounds.Y + Bounds.Height / 2f);
        int scaledWidth = (int)(Bounds.Width * _cardScale);
        int scaledHeight = (int)(Bounds.Height * _cardScale);
        
        return new Rectangle(
            (int)(center.X - scaledWidth / 2f),
            (int)(center.Y - scaledHeight / 2f),
            scaledWidth,
            scaledHeight
        );
    }

    private Rectangle GetScaledImageArea(Rectangle scaledBounds)
    {
        float scaleRatio = (float)scaledBounds.Width / Bounds.Width;
        int scaledImageSize = (int)(IMAGE_SIZE * scaleRatio);
        int scaledMargin = (int)(MARGIN * scaleRatio);
        
        return new Rectangle(
            scaledBounds.X + scaledMargin,
            scaledBounds.Y + scaledMargin + (int)(30 * scaleRatio), // Leave space for title
            scaledImageSize,
            scaledImageSize
        );
    }

    private Rectangle GetScaledTextArea(Rectangle scaledBounds)
    {
        float scaleRatio = (float)scaledBounds.Width / Bounds.Width;
        int scaledImageSize = (int)(IMAGE_SIZE * scaleRatio);
        int scaledMargin = (int)(MARGIN * scaleRatio);
        
        return new Rectangle(
            scaledBounds.X + scaledImageSize + (scaledMargin * 2),
            scaledBounds.Y + scaledMargin + (int)(30 * scaleRatio),
            scaledBounds.Width - scaledImageSize - (scaledMargin * 3),
            scaledBounds.Height - scaledMargin - (int)(30 * scaleRatio)
        );
    }

    private void DrawChoiceButtons(SpriteBatch spriteBatch, Rectangle scaledBounds)
    {
        Rectangle scaledTextArea = GetScaledTextArea(scaledBounds);
        float scaleRatio = (float)scaledBounds.Width / Bounds.Width;
        
        for (int i = 0; i < CurrentChoices.Count && i < _choiceButtonAreas.Count; i++)
        {
            var choice = CurrentChoices[i];
            
            // Get button animation values
            float buttonScale = _buttonScales.ContainsKey(i) ? _buttonScales[i] : 1f;
            float glowIntensity = _buttonGlowIntensity.ContainsKey(i) ? _buttonGlowIntensity[i] : 0f;
            
            // Calculate scaled and animated button area
            Rectangle baseButtonArea = new Rectangle(
                scaledTextArea.X,
                scaledTextArea.Bottom - (CurrentChoices.Count - i) * (int)((CHOICE_BUTTON_HEIGHT + CHOICE_BUTTON_SPACING) * scaleRatio),
                scaledTextArea.Width,
                (int)(CHOICE_BUTTON_HEIGHT * scaleRatio)
            );
            
            // Apply button scale animation
            Vector2 buttonCenter = new Vector2(
                baseButtonArea.X + baseButtonArea.Width / 2f,
                baseButtonArea.Y + baseButtonArea.Height / 2f
            );
            
            int animatedWidth = (int)(baseButtonArea.Width * buttonScale);
            int animatedHeight = (int)(baseButtonArea.Height * buttonScale);
            
            Rectangle animatedButtonArea = new Rectangle(
                (int)(buttonCenter.X - animatedWidth / 2f),
                (int)(buttonCenter.Y - animatedHeight / 2f),
                animatedWidth,
                animatedHeight
            );
            
            // Draw glow effect for hovered/pressed buttons
            if (glowIntensity > 0f)
            {
                Rectangle glowArea = new Rectangle(
                    animatedButtonArea.X - 2,
                    animatedButtonArea.Y - 2,
                    animatedButtonArea.Width + 4,
                    animatedButtonArea.Height + 4
                );
                spriteBatch.Draw(_pixelTexture, glowArea, Color.Yellow * (0.3f * glowIntensity * _fadeAlpha));
            }
            
            // Button background with enhanced colors for hover/press states
            Color baseButtonColor = Color.DarkGray * 0.6f;
            if (_hoveredChoiceIndex == i)
            {
                baseButtonColor = Color.Gray * 0.8f;
            }
            if (_pressedButtonIndex == i && _buttonPressTimer > 0f)
            {
                baseButtonColor = Color.LightGray * 0.9f;
            }
            
            spriteBatch.Draw(_pixelTexture, animatedButtonArea, baseButtonColor * _fadeAlpha);
            
            // Button border with enhanced colors
            Color borderColor = Color.Gray;
            if (_hoveredChoiceIndex == i)
            {
                borderColor = Color.White;
            }
            if (_pressedButtonIndex == i && _buttonPressTimer > 0f)
            {
                borderColor = Color.Yellow;
            }
            
            DrawBorder(spriteBatch, animatedButtonArea, borderColor * _fadeAlpha, 1);
            
            // Button text with enhanced colors and scaling
            string buttonText = $"> {choice.Text}";
            Vector2 textSize = Font.MeasureString(buttonText);
            Vector2 textPos = new Vector2(
                animatedButtonArea.X + (int)(10 * scaleRatio), // Left-aligned with padding
                animatedButtonArea.Y + (animatedButtonArea.Height - textSize.Y) / 2
            );
            
            Color textColor = Color.White;
            if (_hoveredChoiceIndex == i)
            {
                textColor = Color.Yellow;
            }
            if (_pressedButtonIndex == i && _buttonPressTimer > 0f)
            {
                textColor = Color.Orange;
            }
            
            spriteBatch.DrawString(Font, buttonText, textPos, textColor * _fadeAlpha);
        }
    }

    private void DrawTimeoutIndicator(SpriteBatch spriteBatch, Rectangle scaledBounds)
    {
        float scaleRatio = (float)scaledBounds.Width / Bounds.Width;
        int scaledMargin = (int)(MARGIN * scaleRatio);
        
        // Draw timeout progress bar with scaling and fade
        Rectangle timeoutArea = new Rectangle(
            scaledBounds.X + scaledMargin,
            scaledBounds.Bottom - (int)(30 * scaleRatio),
            scaledBounds.Width - (scaledMargin * 2),
            (int)(10 * scaleRatio)
        );
        
        // Background
        spriteBatch.Draw(_pixelTexture, timeoutArea, Color.Black * (0.5f * _fadeAlpha));
        
        // Progress fill
        float progress = CurrentTimeout / AutoTimeoutSeconds;
        int fillWidth = (int)(timeoutArea.Width * progress);
        Rectangle fillArea = new Rectangle(
            timeoutArea.X,
            timeoutArea.Y,
            fillWidth,
            timeoutArea.Height
        );
        
        Color progressColor = CurrentTimeout <= 5f ? Color.Red : Color.Yellow;
        
        // Add pulsing effect when timeout is critical
        float pulseIntensity = 1f;
        if (CurrentTimeout <= 5f)
        {
            pulseIntensity = 0.7f + (MathF.Sin(_pulseTimer * 3f) * 0.3f);
        }
        
        spriteBatch.Draw(_pixelTexture, fillArea, progressColor * (_fadeAlpha * pulseIntensity));
        
        // Border
        DrawBorder(spriteBatch, timeoutArea, Color.White * _fadeAlpha, 1);
        
        // Timeout text
        string timeoutText = $"Timeout: {CurrentTimeout:F0}s";
        Vector2 timeoutTextPos = new Vector2(
            timeoutArea.Right - Font.MeasureString(timeoutText).X,
            timeoutArea.Y - (int)(20 * scaleRatio)
        );
        spriteBatch.DrawString(Font, timeoutText, timeoutTextPos, progressColor * _fadeAlpha);
    }

    private void DrawTimeoutFeedback(SpriteBatch spriteBatch, Rectangle scaledBounds)
    {
        if (!_timeoutOccurred || _timeoutFeedbackTimer <= 0f)
            return;
        
        // Calculate fade effect based on remaining time
        float alpha = (_timeoutFeedbackTimer / TIMEOUT_FEEDBACK_DURATION) * _fadeAlpha;
        
        // Draw feedback message
        string feedbackText = "Choice automatically selected due to timeout!";
        Vector2 textSize = Font.MeasureString(feedbackText);
        
        // Position at top center of the scaled card
        Vector2 feedbackPos = new Vector2(
            scaledBounds.X + (scaledBounds.Width - textSize.X) / 2,
            scaledBounds.Y + (int)(40 * ((float)scaledBounds.Width / Bounds.Width))
        );
        
        // Draw background for better readability
        Rectangle backgroundArea = new Rectangle(
            (int)feedbackPos.X - 10,
            (int)feedbackPos.Y - 5,
            (int)textSize.X + 20,
            (int)textSize.Y + 10
        );
        
        spriteBatch.Draw(_pixelTexture, backgroundArea, Color.Black * (0.8f * alpha));
        DrawBorder(spriteBatch, backgroundArea, Color.Red * alpha, 1);
        
        // Draw feedback text with fade effect
        spriteBatch.DrawString(Font, feedbackText, feedbackPos, Color.Red * alpha);
    }

    private void DrawWrappedText(SpriteBatch spriteBatch, string text, Rectangle area, Color color)
    {
        if (string.IsNullOrEmpty(text) || Font == null)
            return;
        
        // Simple text wrapping - split by words and fit within area width
        string[] words = text.Split(' ');
        List<string> lines = new List<string>();
        string currentLine = "";
        
        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            Vector2 testSize = Font.MeasureString(testLine);
            
            if (testSize.X <= area.Width)
            {
                currentLine = testLine;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    // Word is too long for the line, add it anyway
                    lines.Add(word);
                }
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
        
        // Draw lines
        float lineHeight = Font.MeasureString("A").Y + 2;
        for (int i = 0; i < lines.Count; i++)
        {
            Vector2 linePos = new Vector2(
                area.X,
                area.Y + (i * lineHeight)
            );
            
            // Stop if we exceed the area height
            if (linePos.Y + lineHeight > area.Bottom)
                break;
            
            spriteBatch.DrawString(Font, lines[i], linePos, color);
        }
    }

    private void DrawFallbackContent(SpriteBatch spriteBatch)
    {
        // Draw simple colored rectangles when no font is available
        
        // Image placeholder
        spriteBatch.Draw(_pixelTexture, ImageArea, Color.DarkBlue);
        DrawBorder(spriteBatch, ImageArea, Color.White, 2);
        
        // Text area placeholder
        Rectangle textPlaceholder = new Rectangle(
            TextArea.X,
            TextArea.Y,
            TextArea.Width,
            TextArea.Height / 2
        );
        spriteBatch.Draw(_pixelTexture, textPlaceholder, Color.DarkGreen * 0.3f);
        DrawBorder(spriteBatch, textPlaceholder, Color.Green, 1);
        
        // Choice button placeholders
        for (int i = 0; i < CurrentChoices.Count && i < 4; i++)
        {
            Rectangle buttonPlaceholder = new Rectangle(
                TextArea.X,
                TextArea.Y + TextArea.Height / 2 + 20 + (i * 50),
                TextArea.Width,
                40
            );
            
            Color buttonColor = _hoveredChoiceIndex == i ? Color.Yellow : Color.Gray;
            spriteBatch.Draw(_pixelTexture, buttonPlaceholder, buttonColor * 0.7f);
            DrawBorder(spriteBatch, buttonPlaceholder, Color.White, 1);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, area.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.X, area.Y, thickness, area.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
    }

    public void Hide()
    {
        // Start fade-out animation instead of immediately hiding
        _isAnimatingOut = true;
        _isAnimatingIn = false;
        
        System.Console.WriteLine("[EventCardBox] Starting dialogue close animation");
    }

    public void ForceHide()
    {
        // Immediate hide without animation (for cleanup)
        IsVisible = false;
        CurrentNode = null;
        CurrentChoices.Clear();
        _choiceButtonAreas.Clear();
        _hoveredChoiceIndex = -1;
        _timeoutOccurred = false;
        _timeoutFeedbackTimer = 0f;
        _isAnimatingIn = false;
        _isAnimatingOut = false;
        _fadeAlpha = 0f;
        _cardScale = MIN_SCALE;
        _backgroundDimAlpha = 0f;
        
        // Clear button animation states
        _buttonScales.Clear();
        _buttonGlowIntensity.Clear();
        _pressedButtonIndex = -1;
        _buttonPressTimer = 0f;
        
        // Trigger dialogue closed event
        DialogueClosed?.Invoke();
        
        System.Console.WriteLine("[EventCardBox] Dialogue force closed");
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}

// Data structures for dialogue system

/// <summary>
/// Represents a single node in a dialogue tree containing quest text and available choices
/// </summary>
public class DialogueNode
{
    /// <summary>
    /// Unique identifier for this dialogue node
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Main quest/dialogue text displayed at the top of the EventCardBox
    /// </summary>
    public string QuestText { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the NPC or speaker for this dialogue
    /// </summary>
    public string SpeakerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Filename of the event image to display (e.g., "demo.png")
    /// Path will be resolved to /Content/events/{EventImage}
    /// </summary>
    public string EventImage { get; set; } = string.Empty;
    
    /// <summary>
    /// List of dialogue choices available to the player
    /// </summary>
    public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
    
    /// <summary>
    /// True if this node ends the conversation
    /// </summary>
    public bool IsEndNode { get; set; } = false;
    
    /// <summary>
    /// Optional title for the dialogue (displayed at top of card)
    /// </summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Represents a dialogue choice option that the player can select
/// </summary>
public class DialogueChoice
{
    /// <summary>
    /// Text displayed on the choice button
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the next dialogue node to transition to (null for conversation end)
    /// </summary>
    public string NextNodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Stat rewards to apply when this choice is selected
    /// Key: stat name (e.g., "experience", "mood", "health")
    /// Value: amount to add/subtract
    /// </summary>
    public Dictionary<string, int> StatRewards { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Quest ID to complete or progress when this choice is selected
    /// </summary>
    public string QuestUpdate { get; set; } = string.Empty;
    
    /// <summary>
    /// Journal entry message to add when this choice is selected
    /// </summary>
    public string JournalEntry { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional conditions that must be met for this choice to be available
    /// Simple format: "stat:value" (e.g., "experience:10" means requires 10+ experience)
    /// </summary>
    public List<string> Conditions { get; set; } = new List<string>();
}