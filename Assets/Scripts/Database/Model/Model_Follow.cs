using MongoDB.Bson;
using MongoDB.Driver;

public class Model_Follow
{
    public ObjectId _id;

    public MongoDBRef Sender;
    public MongoDBRef Target;
}
