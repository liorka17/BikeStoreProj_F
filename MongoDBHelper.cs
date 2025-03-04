using MongoDB.Bson; // עבודה עם מסמכי BSON של MongoDB (מבנה הנתונים הבסיסי של MongoDB)
using MongoDB.Driver; // ספרייה לניהול חיבורי MongoDB וביצוע שאילתות
using System.Collections.Generic; // שימוש במבני נתונים כמו רשימות ומילונים (List, Dictionary)


public class MongoDBHelper // מחלקה לעבודה עם מסד נתונים MongoDB
{
    private readonly IMongoDatabase _database; // משתנה פרטי לאחסון החיבור למסד הנתונים


    public MongoDBHelper(string connectionString, string databaseName) // בנאי המחלקה לקבלת מחרוזת חיבור ושם מסד נתונים
    {
        var client = new MongoClient(connectionString); // יצירת לקוח MongoDB עם כתובת החיבור שסופקה
        _database = client.GetDatabase(databaseName); // חיבור למסד הנתונים שצוין
    }

    public void Insert<T>(string collectionName, T document) // פונקציה להוספת מסמך חדש לאוסף ב-MongoDB
    {
        var collection = _database.GetCollection<T>(collectionName); // קבלת האוסף המתאים ממסד הנתונים
        collection.InsertOne(document); // הוספת המסמך לאוסף
    }

    public List<T> Select<T>(string collectionName, FilterDefinition<T> filter) // פונקציה לשליפת מסמכים מאוסף ב-MongoDB עם סינון
    {
        var collection = _database.GetCollection<T>(collectionName); // קבלת האוסף המתאים ממסד הנתונים
        return collection.Find(filter).ToList(); // ביצוע שאילתת חיפוש על פי המסנן והחזרת הרשימה כתוצאה
    }

    public long Update<T>(string collectionName, FilterDefinition<T> filter, UpdateDefinition<T> update)
    {
        var collection = _database.GetCollection<T>(collectionName); // קבלת האוסף המתאים ממסד הנתונים
        var result = collection.UpdateOne(filter, update); // ביצוע עדכון על המסמכים שעונים על התנאי
        return result.ModifiedCount; // החזרת מספר הרשומות שעודכנו
    }

    public void Delete<T>(string collectionName, FilterDefinition<T> filter) // פונקציה למחיקת מסמך מאוסף ב-MongoDB
    {
        var collection = _database.GetCollection<T>(collectionName); // קבלת האוסף המתאים ממסד הנתונים
        collection.DeleteOne(filter); // מחיקת המסמך הראשון שמתאים לתנאי החיפוש
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) // פונקציה להחזרת אוסף (Collection) ממסד הנתונים
    {
        return _database.GetCollection<T>(collectionName); // החזרת האוסף לפי שם שנמסר
    }
}
