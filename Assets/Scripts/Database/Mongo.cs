using MongoDB.Driver;
using MongoDB.Bson;
using UnityEngine;
public class Mongo {
    private const string MONGO_URI = "mongodb+srv://Nahuel:Nahuel@cluster0.krxno.gcp.mongodb.net/lobbydb?retryWrites=true&w=majority";
    private const string DATABASE_NAME = "lobbydb";

    private MongoClient client;
    private IMongoDatabase db;
    private IMongoCollection<Model_Account> accounts;
    private IMongoCollection<Model_Follow> follows;


    public void Init() {
        client = new MongoClient(MONGO_URI);
        db = client.GetDatabase(DATABASE_NAME);
        Debug.Log("Database has been initialized");

        accounts = db.GetCollection<Model_Account>("account");
        follows = db.GetCollection<Model_Follow>("follow");
    }
    public void Shutdown() {
        client = null;
        db = null;
    }

    public bool InsertAccount(string username, string password, string email){

        if (!Utility.IsEmail(email)) {
            Debug.Log(email + " is not an email");
            return false;
        }

        if (!Utility.IsUsername(username))
        {
            Debug.Log(username + " is not a valid username");
            return false;
        }

        if (FindAccountByEmail(email) != null)
        {
            Debug.Log(email + " is already being used");
            return false;
        }

        Model_Account newAccount = new Model_Account();
        newAccount.Username = username;
        newAccount.ShaPassword = password;
        newAccount.Email = email;
        newAccount.Discriminator = "0000";

        int rollCount = 0;
        while (FindAccountByUsernameAndDiscriminator(newAccount.Username, newAccount.Discriminator) != null) {
            newAccount.Discriminator = Random.Range(0, 9999).ToString("0000");
            rollCount++;

            if (rollCount > 1000) {
                Debug.Log("Too many rolls, change name");
                return false;
            }
        }

        accounts.InsertOne(newAccount);

        return true;
    }

    public bool InsertFollow(string token, string emailOrUsername) {
        Model_Follow newFollow = new Model_Follow();
        newFollow.Sender = new MongoDBRef("account", FindAccountByToken(token)._id);
        if (!Utility.IsEmail(emailOrUsername)){
            string[] data = emailOrUsername.Split('#');
            if (data[1] != null) {
                Model_Account follow = FindAccountByUsernameAndDiscriminator(data[0], data[1]);
                if (follow != null) {
                    newFollow.Target = new MongoDBRef("account", follow._id);
                }
                else
                {
                    return false;
                }
            }
        }
        else {
            Model_Account follow = FindAccountByEmail(emailOrUsername);
            if (follow != null)
            {
                newFollow.Target = new MongoDBRef("account", follow._id);
            }
            else
            {
                return false;
            }
        }
        if (newFollow.Target != newFollow.Sender) {
            var filter = Builders<Model_Follow>.Filter.And(
                Builders<Model_Follow>.Filter.Eq("Sender", newFollow.Sender),
                Builders<Model_Follow>.Filter.Eq("Target", newFollow.Target));
            if (follows.Find(filter).FirstOrDefault() == null) {
                follows.InsertOne(newFollow);

            }

            return true;
        }
        return false;
    }


    public Model_Account FindAccountByEmail(string email) {
        var filter = Builders<Model_Account>.Filter.Eq("Email", email);
        return accounts.Find(filter).FirstOrDefault();
    }

    public Model_Account FindAccountByUsernameAndDiscriminator(string username, string discriminator) {
        var filter = Builders<Model_Account>.Filter.And(
                Builders<Model_Account>.Filter.Eq("Username", username),
                Builders<Model_Account>.Filter.Eq("Discriminator", discriminator));
        return accounts.Find(filter).FirstOrDefault();
    }

    public Model_Account FindAccountByToken(string token) {
        var filter = Builders<Model_Account>.Filter.Eq("Token", token);
        return accounts.Find(filter).FirstOrDefault();
    }

    public Model_Account LoginAccount(string usernameOrEmail, string password, int cnnId, string token){
        Model_Account myAccount = null;
        var filter = Builders<Model_Account>.Filter.Empty;
        if (Utility.IsEmail(usernameOrEmail))
        {
            filter = Builders<Model_Account>.Filter.And(
                Builders<Model_Account>.Filter.Eq("Email", usernameOrEmail),
                Builders<Model_Account>.Filter.Eq("ShaPassword", password));
            myAccount = accounts.Find(filter).FirstOrDefault();
        }
        else if (Utility.IsUsernameAndDiscriminator(usernameOrEmail)){
            string[] data = usernameOrEmail.Split('#');
            if (data[1] != null) {
                filter = Builders<Model_Account>.Filter.And(
                    Builders<Model_Account>.Filter.Eq("Username", data[0]),
                    Builders<Model_Account>.Filter.Eq("Discriminator", data[1]),
                    Builders<Model_Account>.Filter.Eq("ShaPassword", password));
                myAccount = accounts.Find(filter).FirstOrDefault();
            }
        }

        if (myAccount != null)
        {
            myAccount.ActiveConnection = cnnId;
            myAccount.Token = token;
            myAccount.Status = 1;
            myAccount.LastLogin = System.DateTime.Now;

            accounts.ReplaceOne(filter, myAccount);
        }
        else {

        }

        return myAccount;
    }
}
