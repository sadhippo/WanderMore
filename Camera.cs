using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HiddenHorizons;

public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 2f;
    public Vector2 Origin { get; set; }
    
    private Vector2 _targetPosition;
    private float _followSpeed = 2f;
    private Viewport _viewport;

    public Camera(Viewport viewport)
    {
        _viewport = viewport;
        Origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        Position = Vector2.Zero;
        _targetPosition = Vector2.Zero;
    }
    
    public void UpdateViewport(Viewport viewport)
    {
        _viewport = viewport;
        Origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
    }

    public void Follow(Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Smoothly move camera towards target
        Vector2 difference = _targetPosition - Position;
        Position += difference * _followSpeed * deltaTime;
    }

    public void SnapToPosition(Vector2 position)
    {
        Position = position;
        _targetPosition = position;
    }

    public Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
               Matrix.CreateScale(Zoom) *
               Matrix.CreateTranslation(Origin.X, Origin.Y, 0);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetTransformMatrix()));
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, GetTransformMatrix());
    }
}