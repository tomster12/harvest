public interface ISerdeable<T>
{
    T Serialize();

    void Deserialize(T data);
}
