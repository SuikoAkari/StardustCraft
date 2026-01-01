namespace StardustCraft.Graphics
{
    public interface IGraphicsObject
    {
        public int ID { get; }
        public void Bind();
        public void Unbind();
    }
}
