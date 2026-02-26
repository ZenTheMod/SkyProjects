using Microsoft.Xna.Framework.Graphics;

namespace ZenSkies.Core;

public interface IParticle
{
    public bool IsActive { get; set; }

    public void Update();

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice device);
}
