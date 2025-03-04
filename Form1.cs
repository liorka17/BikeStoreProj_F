using System; // שימוש בפונקציות בסיסיות של .NET
using System.Collections.Generic; // שימוש ברשימות ומילונים
using System.Data; // עבודה עם מסדי נתונים באמצעות DataTable
using System.Drawing; // שימוש ברכיבי עיצוב וצבעים
using System.Net; // עבודה עם רשת וכתובות IP
using System.Net.Sockets; // עבודה עם חיבורי TCP/IP
using System.Windows.Forms; // שימוש ברכיבי ממשק משתמש (WinForms)
using Microsoft.Data.SqlClient; // ספריית SQL Server לעבודה עם מסדי נתונים מבוססי SQL
using MongoDB.Bson; // עבודה עם מסמכי BSON של MongoDB
using MongoDB.Driver; // ספריית MongoDB לניהול מסדי נתונים מבוססי NoSQL
using Newtonsoft.Json; // ספרייה להמרת אובייקטים ל-JSON ולהיפך



namespace BikeStoreProj
{
    public partial class Form1 : Form
    {
        private SqlConnection conn; // חיבור SQL
        private string connStr = "server=MININT-OIFTK0O\\SQLEXPRESS;database=BikeStore;trusted_connection=true;trustservercertificate=true";

        private string mongoConnectionString = "mongodb://localhost:27017"; // חיבור MongoDB
        private string mongoDatabaseName = "BikeStore"; // שם מסד הנתונים MongoDB

        private MongoDBHelper _mongoHelper; // עוזר ל-MongoDB
        private string _selectedDatabase = ""; // בסיס הנתונים שנבחר

        public Form1()
        {
            
            InitializeComponent();
            SetPlaceholders();

            try
            {
                // בחירת בסיס נתונים לפני המשך העבודה
                while (string.IsNullOrEmpty(_selectedDatabase))
                {
                    var result = MessageBox.Show(
                        "Please select a database to use.\nClick 'Yes' for MongoDB or 'No' for SQL.",
                        "Select Database",
                        MessageBoxButtons.YesNoCancel, // הוספת כפתור ביטול
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Yes)
                    {
                        _selectedDatabase = "mongo";
                        MessageBox.Show("You selected MongoDB.");
                    }
                    else if (result == DialogResult.No)
                    {
                        _selectedDatabase = "sql";
                        MessageBox.Show("You selected SQL.");
                    }
                    else if (result == DialogResult.Cancel) // אם המשתמש לוחץ ביטול
                    {
                        MessageBox.Show("Database selection is required. Exiting application.", "Exit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Application.Exit();
                        return;
                    }
                }

                // אתחול חיבורים
                if (_selectedDatabase == "sql")
                {
                    if (conn == null) // בדיקה שהחיבור לא נוצר כבר
                    {
                        conn = new SqlConnection(connStr);
                        conn.Open();
                    }
                }
                else if (_selectedDatabase == "mongo")
                {
                    try
                    {
                        _mongoHelper = new MongoDBHelper(mongoConnectionString, mongoDatabaseName);
                    }
                    catch (Exception mongoEx)
                    {
                        MessageBox.Show($"Failed to connect to MongoDB: {mongoEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                        return;
                    }
                }

                // טעינת נתונים ראשונית
                LoadBikeData();
                LoadYearsToComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }


        // פונקציה לניהול סגירת החיבורים בעת סגירת הטופס
        protected override void OnFormClosing(FormClosingEventArgs e) // אירוע שמופעל כאשר המשתמש סוגר את הטופס
        {
            try
            {
                // אישור סגירה מהמשתמש
                DialogResult result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                                                      MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No) // אם המשתמש לחץ על "לא"
                {
                    e.Cancel = true; // מבטל את סגירת הטופס
                    return; // יציאה מהפונקציה
                }

                // סגירת חיבור SQL אם פתוח
                if (conn != null && conn.State == ConnectionState.Open) // בודק אם החיבור למסד הנתונים פתוח
                {
                    try
                    {
                        conn.Close(); // סגירת החיבור למסד הנתונים
                        conn.Dispose(); // שחרור מלא של המשאב מהזיכרון
                    }
                    catch (Exception ex) // טיפול בשגיאות בזמן סגירת החיבור
                    {
                        MessageBox.Show($"Error closing SQL connection: {ex.Message}", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // MongoDB - אין חיבור רשמי שדורש סגירה, אבל אם יש קריאה מתמשכת כדאי לנקות את המופע
                if (_mongoHelper != null) // אם יש חיבור פעיל ל-MongoDB
                {
                    _mongoHelper = null; // שחרור החיבור ל-MongoDB מהזיכרון
                }
            }
            catch (Exception ex) // טיפול בשגיאות בלתי צפויות בזמן סגירת הטופס
            {
                MessageBox.Show($"Unexpected error while closing the form: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // קריאה לפונקציה הבסיסית של סגירת הטופס
            base.OnFormClosing(e);
        }

        // פונקציה לטעינת נתונים ממסד הנתונים (SQL או MongoDB)
        private void LoadBikeData() // פונקציה לטעינת נתוני אופניים
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedDatabase)) // בדיקה אם מסד הנתונים לא נבחר
                {
                    MessageBox.Show("No database selected. Please select a database before loading data.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // יציאה מהפונקציה אם אין מסד נתונים נבחר
                }

                if (_selectedDatabase == "sql") // אם המשתמש בחר במסד נתונים מסוג SQL
                {
                    // בדיקה שהחיבור למסד הנתונים פתוח
                    if (conn == null || conn.State != ConnectionState.Open)
                    {
                        MessageBox.Show("SQL connection is not available.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; // יציאה מהפונקציה אם החיבור אינו זמין
                    }

                    // שליפת נתונים ושיוכם ל-ComboBox
                    PopulateComboBox("SELECT DISTINCT Type FROM BikeTypes", BikeTypeBox, "Type"); // שליפת סוגי אופניים
                    PopulateComboBox("SELECT DISTINCT BikeSize FROM BikeTypes", BikeSizeBox, "BikeSize"); // שליפת גדלים זמינים
                    PopulateComboBox("SELECT DISTINCT Color FROM BikeTypes", BikeColorBox, "Color"); // שליפת צבעים זמינים
                }
                else if (_selectedDatabase == "mongo") // אם המשתמש בחר במסד נתונים מסוג MongoDB
                {
                    // בדיקה שחיבור MongoDB קיים
                    if (_mongoHelper == null)
                    {
                        MessageBox.Show("MongoDB connection is not available.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; // יציאה מהפונקציה אם החיבור אינו זמין
                    }

                    // שליפת נתונים ושיוכם ל-ComboBox ממונגו
                    PopulateComboBoxMongo("BikeTypes", BikeTypeBox, "Type"); // שליפת סוגי אופניים
                    PopulateComboBoxMongo("BikeTypes", BikeSizeBox, "BikeSize"); // שליפת גדלים זמינים
                    PopulateComboBoxMongo("BikeTypes", BikeColorBox, "Color"); // שליפת צבעים זמינים
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error loading bike data: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // פונקציה לטעינת נתונים ל-ComboBox ממסד SQL
        private void PopulateComboBox(string query, ComboBox comboBox, string fieldName) // פונקציה למילוי ComboBox בנתונים מה-SQL
        {
            try
            {
                // בדיקת חיבור SQL
                if (conn == null || conn.State != ConnectionState.Open) // בדיקה אם החיבור למסד הנתונים אינו זמין
                {
                    MessageBox.Show("SQL connection is not available.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם החיבור אינו זמין
                }

                // בדיקה שה-ComboBox קיים
                if (comboBox == null) // אם הקומבו בוקס לא מאותחל
                {
                    MessageBox.Show("ComboBox reference is null!", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם אין הפניה חוקית ל-ComboBox
                }

                List<string> items = new List<string>(); // יצירת רשימה לאחסון הערכים שישולבו ב-ComboBox

                using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL עם השאילתה שסופקה
                {
                    using (SqlDataReader reader = cmd.ExecuteReader()) // ביצוע השאילתה וקריאת התוצאות
                    {
                        while (reader.Read()) // מעבר על כל השורות שהוחזרו מהשאילתה
                        {
                            if (reader[fieldName] != DBNull.Value) // בדיקה למניעת קריסה אם יש ערך `NULL`
                            {
                                items.Add(reader[fieldName].ToString()); // הוספת הערך מהרשומה לרשימת הפריטים
                            }
                        }
                    }
                }

                // עדכון ה-ComboBox ברשימה החדשה
                comboBox.Items.Clear(); // ניקוי הערכים הקודמים ב-ComboBox
                comboBox.Items.AddRange(items.ToArray()); // הוספת כל הערכים שנקראו ממסד הנתונים
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // פונקציה לטעינת נתונים ל-ComboBox ממסד MongoDB
        private void PopulateComboBoxMongo(string collectionName, ComboBox comboBox, string fieldName) // פונקציה למילוי ComboBox בנתונים מ-MongoDB
        {
            try
            {
                // בדיקת חיבור ל-MongoDB
                if (_mongoHelper == null) // אם חיבור MongoDB לא מאותחל
                {
                    MessageBox.Show("MongoDB connection is not initialized.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם החיבור אינו זמין
                }

                // בדיקה ששם האוסף ושם השדה תקינים
                if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(fieldName)) // בדיקה אם שם האוסף או שם השדה ריקים
                {
                    MessageBox.Show("Invalid collection name or field name.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם הקלט לא תקין
                }

                // בדיקה שה-ComboBox קיים
                if (comboBox == null) // אם ה-ComboBox לא מאותחל
                {
                    MessageBox.Show("ComboBox reference is null!", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם אין הפניה חוקית ל-ComboBox
                }

                // קריאה לנתונים מ-MongoDB
                var documents = _mongoHelper.Select<BsonDocument>(collectionName, FilterDefinition<BsonDocument>.Empty); // שליפת כל המסמכים מהאוסף

                // HashSet למניעת כפילויות
                HashSet<string> uniqueValues = new HashSet<string>(); // יצירת מבנה נתונים ייחודי לשמירת ערכים ללא כפילויות
                List<string> items = new List<string>(); // יצירת רשימה לאחסון הערכים שישולבו ב-ComboBox

                foreach (var doc in documents) // מעבר על כל המסמכים שהתקבלו מהשאילתה
                {
                    if (doc.Contains(fieldName) && doc[fieldName] != null) // בדיקה אם השדה קיים במסמך ואינו ריק
                    {
                        string value = doc[fieldName].ToString(); // המרת הערך למחרוזת

                        // מניעת כפילות והוספה לרשימה
                        if (uniqueValues.Add(value)) // אם הערך חדש (לא נמצא ב-HashSet)
                        {
                            items.Add(value); // הוספת הערך לרשימה
                        }
                    }
                }

                // עדכון ה-ComboBox ברשימה החדשה
                comboBox.Items.Clear(); // ניקוי הערכים הקודמים ב-ComboBox
                comboBox.Items.AddRange(items.ToArray()); // הוספת כל הערכים שנקראו ממסד הנתונים

                // Debug - הצגת הערכים שהוספו
                Console.WriteLine($"Unique values added to ComboBox: {string.Join(", ", uniqueValues)}"); // הדפסת הערכים שנוספו ל-ComboBox בקונסולה
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"Error loading data from MongoDB: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // פונקציה לטעינת נתונים ממסד MongoDB
        private void LoadDataFromMongo() // פונקציה לטעינת נתונים ממסד הנתונים MongoDB
        {
            try
            {
                // בדיקה שחיבור ל-MongoDB פעיל
                if (_mongoHelper == null) // אם החיבור לא מאותחל
                {
                    MessageBox.Show("MongoDB connection is not initialized.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // יציאה מהפונקציה אם החיבור אינו זמין
                }

                // שליפת מסמכים מהאוסף BikeTypes
                var bikes = _mongoHelper.Select<BsonDocument>("BikeTypes", FilterDefinition<BsonDocument>.Empty); // שליפת כל המסמכים מהאוסף

                // בדיקה אם האוסף קיים ומכיל נתונים
                if (bikes == null || !bikes.Any()) // אם האוסף ריק או שלא נמצאו מסמכים
                {
                    MessageBox.Show("No data found in MongoDB collection 'BikeTypes'.", "No Data",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // יציאה מהפונקציה אם אין נתונים
                }

                // יצירת DataTable עם סוגי נתונים נכונים
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("Bike ID", typeof(string)); // עמודת מזהה האופניים
                dataTable.Columns.Add("Bike Type", typeof(string)); // עמודת סוג האופניים
                dataTable.Columns.Add("Bike Size", typeof(string)); // עמודת גודל האופניים
                dataTable.Columns.Add("Bike Color", typeof(string)); // עמודת צבע האופניים
                dataTable.Columns.Add("Stock Quantity", typeof(int)); // עמודת כמות במלאי
                dataTable.Columns.Add("Sale Price", typeof(decimal)); // עמודת מחיר מכירה

                // המרת המסמכים ממונגו לשורות ב-DataTable
                foreach (var bike in bikes) // מעבר על כל המסמכים שהתקבלו ממונגו
                {
                    dataTable.Rows.Add(
                        bike.TryGetValue("BikeID", out var bikeId) ? bikeId.ToString() : "", // הוספת מזהה האופניים
                        bike.TryGetValue("Type", out var type) ? type.ToString() : "", // הוספת סוג האופניים
                        bike.TryGetValue("BikeSize", out var size) ? size.ToString() : "", // הוספת גודל האופניים
                        bike.TryGetValue("Color", out var color) ? color.ToString() : "", // הוספת צבע האופניים
                        bike.TryGetValue("StockQuantity", out var stock) ? stock.AsInt32 : 0, // הוספת כמות במלאי
                        bike.TryGetValue("SalePrice", out var price) ? Convert.ToDecimal(price) : 0.00m // הוספת מחיר המכירה
                    );
                }

                // חיבור ה-DataTable ל-DataGridView
                dataGridViewMain.DataSource = dataTable; // הצגת הנתונים בטבלת המשתמש

                MessageBox.Show($"Data loaded successfully! {dataTable.Rows.Count} rows retrieved from MongoDB.",
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); // הצגת הודעה על טעינה מוצלחת
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"Error loading data from MongoDB: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // פונקציה להוספת לקוח חדש בעת ביצוע הזמנה בלבד
        private object AddNewCustomer(string firstName, string lastName, string phoneNumber, string address, string email) // פונקציה להוספת לקוח חדש
        {
            try
            {
                // ניקוי נתונים כדי למנוע תווים לא תקינים
                string normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber.Trim()); // ניקוי ומניעת רווחים מיותרים במספר הטלפון
                firstName = firstName.Trim(); // הסרת רווחים מיותרים משם פרטי
                lastName = lastName.Trim(); // הסרת רווחים מיותרים משם משפחה
                address = address.Trim(); // הסרת רווחים מיותרים מהכתובת
                email = email.Trim(); // הסרת רווחים מיותרים מהאימייל

                // בדיקה אם מספר הטלפון ריק (עלול לגרום לשגיאת UNIQUE)
                if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
                {
                    MessageBox.Show("Phone number cannot be empty.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null; // יציאה מהפונקציה אם אין מספר טלפון
                }

                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    // בדיקה אם הלקוח כבר קיים במערכת כדי למנוע כפילות
                    object existingCustomer = EnsureCustomerExists(firstName, lastName, normalizedPhoneNumber);
                    if (existingCustomer != null) // אם הלקוח כבר קיים
                    {
                        return existingCustomer; // מחזיר את המזהה שלו
                    }

                    // בדיקה אם החיבור תקין
                    if (conn == null)
                    {
                        MessageBox.Show("SQL connection is not initialized.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null; // יציאה אם החיבור למסד הנתונים לא מאותחל
                    }

                    if (conn.State != ConnectionState.Open) // אם החיבור למסד הנתונים לא פתוח
                    {
                        conn.Open(); // פתיחת החיבור
                    }

                    string insertQuery = @"
            INSERT INTO Customers (FirstName, LastName, PhoneNumber, Address, Email) 
            OUTPUT INSERTED.CustomerID 
            VALUES (@FirstName, @LastName, @PhoneNumber, @Address, @Email)"; // שאילתת SQL להוספת לקוח חדש והחזרת ה-ID שלו

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn)) // יצירת פקודת SQL להוספת הלקוח
                    {
                        cmd.Parameters.AddWithValue("@FirstName", firstName);
                        cmd.Parameters.AddWithValue("@LastName", lastName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", normalizedPhoneNumber);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@Email", email);

                        var result = cmd.ExecuteScalar(); // ביצוע השאילתה והחזרת מזהה הלקוח

                        if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int customerId))
                        {
                            return customerId; // מחזיר את מזהה הלקוח החדש
                        }
                        else
                        {
                            MessageBox.Show("Failed to retrieve CustomerID after insertion.", "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return null;
                        }
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    if (_mongoHelper == null) // אם החיבור ל-MongoDB לא מאותחל
                    {
                        MessageBox.Show("MongoDB connection is not initialized.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת האוסף "Customers" ממונגו

                    // בדיקה אם הלקוח כבר קיים ב-MongoDB
                    var filter = Builders<BsonDocument>.Filter.Eq("PhoneNumber", normalizedPhoneNumber); // חיפוש לקוח לפי מספר טלפון
                    var existingCustomer = customersCollection.Find(filter).FirstOrDefault(); // בדיקה אם קיים לקוח עם אותו מספר טלפון
                    if (existingCustomer != null)
                    {
                        return existingCustomer["_id"].ToString(); // מחזיר את מזהה הלקוח הקיים
                    }

                    // יצירת מסמך לקוח חדש
                    var newCustomer = new BsonDocument
            {
                { "FirstName", firstName },
                { "LastName", lastName },
                { "PhoneNumber", normalizedPhoneNumber },
                { "Address", address },
                { "Email", email }
            };

                    customersCollection.InsertOne(newCustomer); // הכנסת הלקוח החדש לאוסף

                    if (newCustomer.Contains("_id")) // אם המסמך מכיל מזהה
                    {
                        return newCustomer["_id"].ToString(); // מחזיר את ObjectId כמחרוזת
                    }
                    else
                    {
                        MessageBox.Show("Failed to retrieve Customer ID from MongoDB.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
                else
                {
                    MessageBox.Show("No database selected. Please select a valid database.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            catch (SqlException sqlEx) // טיפול בשגיאות SQL
            {
                if (sqlEx.Number == 2627) // 2627 = SQL Unique Constraint Violation (הפרת מגבלת ייחודיות)
                {
                    MessageBox.Show($"Customer with this phone number already exists in SQL.\nPhone: {phoneNumber}",
                                    "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show($"SQL Error adding new customer: {sqlEx.Message}", "SQL Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null;
            }
            catch (Exception ex) // טיפול בשגיאות כלליות
            {
                MessageBox.Show($"Error adding new customer: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private object EnsureCustomerExists(string firstName, string lastName, string phoneNumber) // פונקציה לבדיקה אם לקוח קיים במסד הנתונים
        {
            try
            {
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים
                    {
                        conn.Open(); // פתיחה מבוקרת של החיבור

                        string checkQuery = @"
                SELECT CustomerID 
                FROM Customers 
                WHERE PhoneNumber = @PhoneNumber 
                  AND FirstName = @FirstName 
                  AND LastName = @LastName"; // שאילתת SQL לבדיקה אם הלקוח קיים לפי מספר טלפון ושם

                        using (SqlCommand cmd = new SqlCommand(checkQuery, conn)) // יצירת פקודת SQL עם השאילתה שסופקה
                        {
                            cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber); // הוספת מספר הטלפון כפרמטר
                            cmd.Parameters.AddWithValue("@FirstName", firstName); // הוספת שם פרטי כפרמטר
                            cmd.Parameters.AddWithValue("@LastName", lastName); // הוספת שם משפחה כפרמטר

                            var result = cmd.ExecuteScalar(); // ביצוע השאילתה והחזרת מזהה הלקוח אם נמצא
                            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : null; // המרת מזהה הלקוח אם קיים, אחרת החזרת null
                        }
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת אוסף הלקוחות ממונגו

                    var filter = Builders<BsonDocument>.Filter.And( // יצירת שאילתת סינון למציאת לקוח עם אותם פרטים
                        Builders<BsonDocument>.Filter.Eq("FirstName", firstName),
                        Builders<BsonDocument>.Filter.Eq("LastName", lastName),
                        Builders<BsonDocument>.Filter.Eq("PhoneNumber", phoneNumber)
                    );

                    var existingCustomer = customersCollection.Find(filter).FirstOrDefault(); // חיפוש הלקוח במסד הנתונים

                    // בדיקה בטוחה שהמסמך קיים ושהשדה "_id" באמת קיים
                    return (existingCustomer != null && existingCustomer.Contains("_id"))
                        ? existingCustomer["_id"].ToString() // אם נמצא, החזרת המזהה של הלקוח
                        : null; // אחרת החזרת null
                }

                return null; // אם לא נבחר מסד נתונים מתאים, מוחזר null
            }
            catch (Exception ex) // טיפול בשגיאות
            {
                MessageBox.Show($"Error checking customer existence: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null; // במקרה של שגיאה מוחזר null
            }
        }

        // פונקציה לעדכון פרטי לקוח קיים
        private void UpdateCustomerDetails(object customerId, string firstName, string lastName, string phoneNumber, string address, string email) // פונקציה לעדכון פרטי לקוח
        {
            if (customerId == null) // בדיקה אם מזהה הלקוח ריק
            {
                MessageBox.Show("Customer ID cannot be null.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // יציאה מהפונקציה אם המזהה ריק
            }

            string normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber); // נרמול מספר הטלפון כדי למנוע שגיאות או כפילויות

            if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
            {
                using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                {
                    conn.Open(); // פתיחת החיבור - חיבור מנוהל כדי למנוע זליגות זיכרון

                    string query = @"
            UPDATE Customers 
            SET FirstName = @FirstName, 
                LastName = @LastName, 
                PhoneNumber = @PhoneNumber, 
                Address = @Address, 
                Email = @Email 
            WHERE CustomerID = @CustomerID"; // שאילתת SQL לעדכון פרטי הלקוח לפי ה-ID שלו

                    using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                    {
                        cmd.Parameters.AddWithValue("@CustomerID", Convert.ToInt32(customerId)); // המרה בטוחה של מזהה הלקוח למספר שלם
                        cmd.Parameters.AddWithValue("@FirstName", firstName); // הכנסת השם הפרטי כפרמטר
                        cmd.Parameters.AddWithValue("@LastName", lastName); // הכנסת שם המשפחה כפרמטר
                        cmd.Parameters.AddWithValue("@PhoneNumber", normalizedPhoneNumber); // הכנסת מספר הטלפון כפרמטר
                        cmd.Parameters.AddWithValue("@Address", address); // הכנסת כתובת כפרמטר
                        cmd.Parameters.AddWithValue("@Email", email); // הכנסת אימייל כפרמטר

                        int rowsAffected = cmd.ExecuteNonQuery(); // ביצוע השאילתה ובדיקת מספר הרשומות שעודכנו

                        if (rowsAffected == 0) // אם אף רשומה לא התעדכנה
                        {
                            MessageBox.Show("Customer update failed. No matching record found.", "Warning",
                                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
            {
                var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת אוסף הלקוחות ממונגו

                try
                {
                    var objectId = ObjectId.Parse(customerId.ToString()); // המרה בטוחה של מזהה הלקוח לפורמט ObjectId של MongoDB

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId); // יצירת פילטר למציאת הלקוח לפי ה-ID שלו
                    var update = Builders<BsonDocument>.Update
                        .Set("FirstName", firstName) // עדכון השם הפרטי
                        .Set("LastName", lastName) // עדכון שם המשפחה
                        .Set("PhoneNumber", normalizedPhoneNumber) // עדכון מספר הטלפון
                        .Set("Address", address) // עדכון כתובת
                        .Set("Email", email); // עדכון כתובת אימייל

                    var result = customersCollection.UpdateOne(filter, update); // ביצוע העדכון במסד הנתונים MongoDB

                    if (result.ModifiedCount == 0) // אם אף רשומה לא התעדכנה
                    {
                        MessageBox.Show("Customer update failed in MongoDB. No matching record found.", "Warning",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (FormatException) // טיפול במקרה שבו ה-ID של הלקוח לא בפורמט תקין
                {
                    MessageBox.Show("Invalid Customer ID format for MongoDB.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // פונקציה לבדיקה אם מספר טלפון כבר קיים במסד הנתונים
        private object GetCustomerIdByPhone(string phoneNumber) // פונקציה לקבלת מזהה לקוח לפי מספר טלפון
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) // בדיקה אם מספר הטלפון ריק או מכיל רק רווחים
            {
                MessageBox.Show("Phone number cannot be empty.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null; // יציאה מהפונקציה אם אין מספר טלפון תקין
            }

            string normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber); // נרמול מספר הטלפון כדי למנוע כפילויות וטעויות

            try
            {
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                    {
                        conn.Open(); // פתיחת החיבור למסד הנתונים

                        string query = "SELECT CustomerID FROM Customers WHERE PhoneNumber = @PhoneNumber"; // שאילתת SQL למציאת מזהה הלקוח לפי מספר טלפון
                        using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                        {
                            cmd.Parameters.AddWithValue("@PhoneNumber", normalizedPhoneNumber); // הכנסת מספר הטלפון כפרמטר לשאילתה
                            var result = cmd.ExecuteScalar(); // ביצוע השאילתה וקבלת התוצאה

                            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : null; // אם נמצא מזהה, להמיר למספר, אחרת להחזיר null
                        }
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת אוסף הלקוחות ממונגו
                    var filter = Builders<BsonDocument>.Filter.Eq("PhoneNumber", normalizedPhoneNumber); // יצירת שאילתת חיפוש לפי מספר טלפון

                    var customer = customersCollection.Find(filter).FirstOrDefault(); // חיפוש הלקוח במסד הנתונים
                    return customer != null ? customer["_id"].AsObjectId : null; // אם נמצא, מחזיר את ה-ID שלו, אחרת מחזיר null
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"Error retrieving customer ID: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null; // אם לא נמצא מזהה לקוח, מוחזר null
        }

        //פונקציות עבור הזמנה וטרם ביצוע הזמנה

        private void UpdateCustomerDetailsMongo(string customerId, string firstName, string lastName, string phoneNumber, string address, string email) // פונקציה לעדכון פרטי לקוח ב-MongoDB
        {
            if (string.IsNullOrWhiteSpace(customerId) || !ObjectId.TryParse(customerId, out ObjectId objectId)) // בדיקה אם ה-ID ריק או לא תקין
            {
                MessageBox.Show("Invalid customer ID.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // יציאה מהפונקציה אם ה-ID לא תקין
            }

            var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת אוסף הלקוחות ממונגו
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId); // יצירת פילטר למציאת הלקוח לפי ה-ID שלו

            var updateDefinitionList = new List<UpdateDefinition<BsonDocument>>(); // יצירת רשימה לאחסון כל העדכונים שנעשה

            if (!string.IsNullOrWhiteSpace(firstName)) // אם השם הפרטי לא ריק
                updateDefinitionList.Add(Builders<BsonDocument>.Update.Set("FirstName", firstName)); // הוספת השם הפרטי לרשימת העדכונים

            if (!string.IsNullOrWhiteSpace(lastName)) // אם שם המשפחה לא ריק
                updateDefinitionList.Add(Builders<BsonDocument>.Update.Set("LastName", lastName)); // הוספת שם המשפחה לרשימת העדכונים

            if (!string.IsNullOrWhiteSpace(phoneNumber)) // אם מספר הטלפון לא ריק
                updateDefinitionList.Add(Builders<BsonDocument>.Update.Set("PhoneNumber", phoneNumber)); // הוספת מספר הטלפון לרשימת העדכונים

            if (!string.IsNullOrWhiteSpace(address)) // אם הכתובת לא ריקה
                updateDefinitionList.Add(Builders<BsonDocument>.Update.Set("Address", address)); // הוספת הכתובת לרשימת העדכונים

            if (!string.IsNullOrWhiteSpace(email)) // אם האימייל לא ריק
                updateDefinitionList.Add(Builders<BsonDocument>.Update.Set("Email", email)); // הוספת האימייל לרשימת העדכונים

            if (updateDefinitionList.Count == 0) // אם לא הוזן אף ערך לעדכון
            {
                MessageBox.Show("No valid fields provided for update.", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // יציאה מהפונקציה אם אין נתונים לעדכן
            }

            var updateDefinition = Builders<BsonDocument>.Update.Combine(updateDefinitionList); // שילוב כל העדכונים לרשימה אחת

            var result = customersCollection.UpdateOne(filter, updateDefinition); // ביצוע העדכון במסד הנתונים MongoDB

            if (result.ModifiedCount > 0) // בדיקה אם לפחות רשומה אחת התעדכנה
            {
                MessageBox.Show("Customer details updated successfully.", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No changes made or customer not found.", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PreviewOrderBeforeSubmitingBtn_Click(object sender, EventArgs e) // פונקציה להצגת תצוגה מקדימה של ההזמנה לפני שליחתה
        {
            try
            {
                // 📌 בדיקת מילוי כל השדות לפני המשך
                if (string.IsNullOrWhiteSpace(FirstName.Text) ||
                    string.IsNullOrWhiteSpace(LastName.Text) ||
                    string.IsNullOrWhiteSpace(PhoneNumber.Text) ||
                    string.IsNullOrWhiteSpace(Adress.Text) ||
                    string.IsNullOrWhiteSpace(EmailBox.Text) ||
                    BikeTypeBox.SelectedItem == null ||
                    BikeSizeBox.SelectedItem == null ||
                    BikeColorBox.SelectedItem == null)
                {
                    MessageBox.Show("Please fill all required fields before proceeding.", "Missing Information",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // יציאה מהפונקציה אם אחד מהשדות ריק
                }

                // 📌 איסוף נתוני לקוח
                string firstName = FirstName.Text.Trim(); // ניקוי רווחים מהשם הפרטי
                string lastName = LastName.Text.Trim(); // ניקוי רווחים משם המשפחה
                string phoneNumber = NormalizePhoneNumber(PhoneNumber.Text.Trim()); // נרמול מספר הטלפון
                string address = Adress.Text.Trim(); // ניקוי רווחים מהכתובת
                string email = EmailBox.Text.Trim(); // ניקוי רווחים מהאימייל

                object customerId = EnsureCustomerExists(firstName, lastName, phoneNumber); // בדיקה אם הלקוח כבר קיים במערכת

                if (customerId == null) // אם הלקוח לא קיים, מוסיפים אותו
                {
                    customerId = AddNewCustomer(firstName, lastName, phoneNumber, address, email);
                    if (customerId == null) // אם הוספת הלקוח נכשלה
                    {
                        MessageBox.Show("Failed to add new customer. Please try again.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else // אם הלקוח כבר קיים, שואלים אם לעדכן את פרטיו
                {
                    DialogResult result = MessageBox.Show("Customer already exists. Would you like to update the details?",
                                                          "Customer Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes) // אם המשתמש בחר לעדכן פרטים
                    {
                        if (_selectedDatabase == "sql") // עדכון ב-SQL
                        {
                            if (int.TryParse(customerId.ToString(), out int parsedCustomerId))
                            {
                                UpdateCustomerDetails(parsedCustomerId, firstName, lastName, phoneNumber, address, email);
                            }
                            else
                            {
                                MessageBox.Show("Invalid Customer ID format.", "Error",
                                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else if (_selectedDatabase == "mongo") // עדכון ב-MongoDB
                        {
                            UpdateCustomerDetailsMongo(customerId.ToString(), firstName, lastName, phoneNumber, address, email);
                        }
                    }
                }

                // 📌 איסוף פרטי האופניים
                string bikeType = BikeTypeBox.SelectedItem.ToString().Trim(); // קבלת סוג האופניים שנבחר
                string bikeSize = BikeSizeBox.SelectedItem.ToString().Trim(); // קבלת גודל האופניים שנבחר
                string bikeColor = BikeColorBox.SelectedItem.ToString().Trim(); // קבלת צבע האופניים שנבחר
                int quantity = (int)QuantitySelector.Value; // קבלת כמות האופניים

                if (quantity <= 0) // בדיקה אם הכמות חוקית
                {
                    MessageBox.Show("Quantity must be at least 1.", "Invalid Quantity",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal unitPrice = GetBikePrice(bikeType, bikeSize, bikeColor); // קבלת המחיר ליחידה של הדגם שנבחר

                if (unitPrice <= 0) // בדיקה אם המחיר חוקי
                {
                    MessageBox.Show("Invalid price retrieved for the selected bike. Please check the database.", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                decimal totalPrice = unitPrice * quantity; // חישוב המחיר הכולל להזמנה

                // 📌 טיפול במסד הנתונים: SQL מול MongoDB
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    if (conn == null) // בדיקה שהחיבור למסד הנתונים קיים
                    {
                        conn = new SqlConnection(connStr);
                        conn.Open();
                    }
                    else if (conn.State != ConnectionState.Open) // אם החיבור קיים אבל סגור
                    {
                        conn.Open();
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    if (_mongoHelper == null) // אם החיבור ל-MongoDB לא מאותחל
                    {
                        MessageBox.Show("MongoDB connection is not initialized!", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 📌 הצגת סיכום ההזמנה
                string orderDetails =
                    $"Customer Name: {firstName} {lastName}\n" +
                    $"Phone Number: {phoneNumber}\n" +
                    $"Address: {address}\n" +
                    $"Email: {email}\n\n" +
                    $"Bike Type: {bikeType}\n" +
                    $"Bike Size: {bikeSize}\n" +
                    $"Bike Color: {bikeColor}\n" +
                    $"Quantity: {quantity}\n" +
                    $"Unit Price: {unitPrice:C}\n" +
                    $"Total Price: {totalPrice:C}";

                MessageBox.Show(orderDetails, "Order Preview",
                                MessageBoxButtons.OK, MessageBoxIcon.Information); // הצגת תצוגה מקדימה של ההזמנה
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int? GetBikeID(string bikeType, string bikeSize, string bikeColor) // פונקציה לשליפת מזהה האופניים לפי סוג, גודל וצבע
        {
            try
            {
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    // 📌 בדיקה שהחיבור תקין לפני ניסיון שאילתא
                    if (conn == null || conn.State != ConnectionState.Open) // אם החיבור למסד הנתונים לא פתוח
                    {
                        conn = new SqlConnection(connStr); // יצירת חיבור חדש
                        conn.Open(); // פתיחת החיבור
                    }

                    string query = @"
            SELECT BikeID
            FROM BikeTypes
            WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color"; // שאילתת SQL לשליפת מזהה האופניים

                    using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                    {
                        cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים כפרמטר

                        var result = cmd.ExecuteScalar(); // ביצוע השאילתה וקבלת התוצאה
                        return result != null ? (int?)Convert.ToInt32(result) : null; // אם נמצא מזהה, להמיר למספר, אחרת להחזיר null
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    if (_mongoHelper == null) // בדיקה אם החיבור ל-MongoDB מאותחל
                    {
                        MessageBox.Show("MongoDB connection is not initialized!", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    var filter = Builders<BsonDocument>.Filter.And( // יצירת פילטר למציאת האופניים לפי סוג, גודל וצבע
                        Builders<BsonDocument>.Filter.Eq("Type", bikeType),
                        Builders<BsonDocument>.Filter.Eq("BikeSize", bikeSize),
                        Builders<BsonDocument>.Filter.Eq("Color", bikeColor)
                    );

                    var collection = _mongoHelper.GetCollection<BsonDocument>("BikeTypes"); // קבלת אוסף האופניים ממונגו
                    var bike = collection.Find(filter).FirstOrDefault(); // חיפוש רשומת האופניים המתאימה

                    if (bike != null && bike.Contains("BikeID")) // בדיקה אם נמצאה רשומת האופניים והיא מכילה מזהה
                    {
                        var bikeIdValue = bike["BikeID"];

                        // ✅ טיפול בכל סוגי ה-ID האפשריים
                        if (bikeIdValue.BsonType == BsonType.Int32) // אם ה-ID הוא מספר שלם 32 ביט
                            return bikeIdValue.AsInt32;
                        else if (bikeIdValue.BsonType == BsonType.Int64) // אם ה-ID הוא מספר שלם 64 ביט
                            return (int)bikeIdValue.AsInt64;
                        else if (bikeIdValue.BsonType == BsonType.String && int.TryParse(bikeIdValue.AsString, out int parsedId)) // אם ה-ID הוא מחרוזת
                            return parsedId;
                        else if (bikeIdValue.BsonType == BsonType.ObjectId) // אם ה-ID הוא ObjectId של MongoDB
                            return int.Parse(bikeIdValue.AsObjectId.ToString().Substring(0, 6), System.Globalization.NumberStyles.HexNumber);
                    }

                    MessageBox.Show("BikeID not found in MongoDB for the selected type, size, and color.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"Error retrieving BikeID: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null; // במקרה של תקלה מוחזר null
        }

        private int GetBikeStock(string bikeType, string bikeSize, string bikeColor) // פונקציה לקבלת כמות המלאי של אופניים לפי סוג, גודל וצבע
        {
            try
            {
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    // 📌 בדיקה שהחיבור תקין לפני ניסיון שאילתא
                    if (conn == null || conn.State != ConnectionState.Open) // אם החיבור לא פתוח
                    {
                        conn = new SqlConnection(connStr); // יצירת חיבור חדש למסד הנתונים
                        conn.Open(); // פתיחת החיבור
                    }

                    string query = @"
            SELECT StockQuantity 
            FROM BikeTypes 
            WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color"; // שאילתת SQL לשליפת כמות המלאי לפי הפרמטרים שנבחרו

                    using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                    {
                        cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים כפרמטר

                        object result = cmd.ExecuteScalar(); // ביצוע השאילתה וקבלת התוצאה

                        if (result != null && int.TryParse(result.ToString(), out int stock)) // אם נמצאה תוצאה תקינה
                        {
                            return stock; // החזרת כמות המלאי
                        }
                        else
                        {
                            MessageBox.Show("Stock quantity not found or invalid in SQL.", "Warning",
                                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return 0; // אם אין כמות תקפה, מחזירים 0
                        }
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    if (_mongoHelper == null) // בדיקה אם החיבור ל-MongoDB מאותחל
                    {
                        MessageBox.Show("MongoDB connection is not initialized!", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return 0;
                    }

                    var filter = Builders<BsonDocument>.Filter.And( // יצירת פילטר למציאת אופניים לפי סוג, גודל וצבע
                        Builders<BsonDocument>.Filter.Eq("Type", bikeType),
                        Builders<BsonDocument>.Filter.Eq("BikeSize", bikeSize),
                        Builders<BsonDocument>.Filter.Eq("Color", bikeColor)
                    );

                    var collection = _mongoHelper.GetCollection<BsonDocument>("BikeTypes"); // קבלת אוסף האופניים ממונגו
                    var bike = collection.Find(filter).FirstOrDefault(); // חיפוש רשומת האופניים המתאימה

                    if (bike != null && bike.Contains("StockQuantity")) // בדיקה אם נמצאה רשומת האופניים והיא מכילה מלאי
                    {
                        var stockValue = bike["StockQuantity"];

                        // ✅ טיפול בכל סוגי הנתונים האפשריים
                        if (stockValue.BsonType == BsonType.Int32) // אם המלאי הוא מספר שלם 32 ביט
                            return stockValue.AsInt32;
                        else if (stockValue.BsonType == BsonType.Int64) // אם המלאי הוא מספר שלם 64 ביט
                            return (int)stockValue.AsInt64;
                        else if (stockValue.BsonType == BsonType.Double) // אם המלאי הוא מספר מסוג Double
                            return (int)stockValue.AsDouble;
                        else if (stockValue.BsonType == BsonType.Decimal128) // אם המלאי הוא מספר מסוג Decimal128
                            return (int)stockValue.AsDecimal128;

                        MessageBox.Show("StockQuantity format in MongoDB is unsupported.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("StockQuantity not found in MongoDB for the selected bike.", "Warning",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error retrieving bike stock: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 0; // במקרה של כשל, מוחזר 0
        }
        
        //הזמנה במונגו
        private void SubmitOrder(int bikeID, int quantity) // פונקציה לשליחת הזמנה
        {
            try
            {
                // 🔍 שליפת נתוני הלקוח
                string firstName = FirstName.Text.Trim(); // ניקוי רווחים משם פרטי
                string lastName = LastName.Text.Trim(); // ניקוי רווחים משם משפחה
                string phoneNumber = PhoneNumber.Text.Trim(); // ניקוי רווחים ממספר טלפון
                string address = Adress.Text.Trim(); // ניקוי רווחים מהכתובת
                string email = EmailBox.Text.Trim(); // ניקוי רווחים מהאימייל

                // 📌 חישוב מחיר יחידה וסכום כולל
                decimal unitPrice = GetBikePrice(BikeTypeBox.SelectedItem.ToString(),
                                                 BikeSizeBox.SelectedItem.ToString(),
                                                 BikeColorBox.SelectedItem.ToString()); // קבלת מחיר ליחידה
                decimal totalPrice = unitPrice * quantity; // חישוב המחיר הכולל להזמנה

                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    // 🗄️ SQL - שליחת פקודת ההזמנה לשרת
                    string serverIp = "127.0.0.1"; // כתובת השרת
                    int serverPort = 5000; // פורט השרת

                    var command = new Command // יצירת אובייקט פקודה לשליחה לשרת
                    {
                        CommandID = 1,
                        Action = "submit_order", // פעולה להזמנה
                        Table = "Orders", // שם הטבלה
                        Parameters = new Dictionary<string, object> // מילון עם הפרמטרים של ההזמנה
                        {
                            { "FirstName", firstName },
                            { "LastName", lastName },
                            { "PhoneNumber", phoneNumber },
                            { "Address", address },
                            { "Email", email },
                            { "BikeID", bikeID },
                            { "Quantity", quantity },
                            { "UnitPrice", unitPrice },
                            { "TotalPrice", totalPrice }
                        },
                        DatabaseClient = "sql"
                    };

                    string response = SendCommand(serverIp, serverPort, command); // שליחת הפקודה לשרת וקבלת תשובה
                    if (!response.ToLower().Contains("success")) // בדיקה אם ההזמנה נכשלה
                    {
                        MessageBox.Show($"Failed to submit order to SQL: {response}", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    // 🗄️ MongoDB - הוספת המסמך לאוסף "Orders"
                    var orderDocument = new BsonDocument
                    {
                        { "FirstName", firstName },
                        { "LastName", lastName },
                        { "PhoneNumber", phoneNumber },
                        { "Address", address },
                        { "Email", email },
                        { "BikeID", bikeID },
                        { "Quantity", quantity },
                        { "UnitPrice", unitPrice },
                        { "TotalPrice", totalPrice },
                        { "OrderDate", DateTime.Now } // הוספת תאריך ביצוע ההזמנה
                    };

                    _mongoHelper.Insert("Orders", orderDocument); // הכנסת המסמך למסד הנתונים MongoDB
                }

                MessageBox.Show("Order submitted successfully!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information); // הודעה על הצלחה
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error submitting order: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable ConvertBsonToDataTable(List<BsonDocument> bsonList) // פונקציה להמרת רשימת BsonDocument לטבלת DataTable
        {
            DataTable table = new DataTable(); // יצירת אובייקט DataTable ריק

            if (bsonList == null || bsonList.Count == 0) // בדיקה אם הרשימה ריקה או לא תקפה
                return table; // החזרת טבלה ריקה אם אין נתונים

            // יצירת עמודות בטבלה על פי המפתחות במסמך הראשון
            foreach (var key in bsonList[0].Names) // מעבר על כל השמות (Keys) במסמך הראשון ברשימה
            {
                table.Columns.Add(key, typeof(string)); // יצירת עמודה חדשה בטבלה, ברירת מחדל - מחרוזת
            }

            // עיבוד כל המסמכים והמרתם לשורות בטבלה
            foreach (var doc in bsonList) // מעבר על כל המסמכים ברשימה
            {
                DataRow row = table.NewRow(); // יצירת שורה חדשה בטבלה

                foreach (var key in doc.Names) // מעבר על כל השדות (Keys) במסמך הנוכחי
                {
                    var value = doc[key]; // קבלת הערך מהמסמך

                    if (value.IsBsonNull) // אם הערך הוא Null
                    {
                        row[key] = DBNull.Value; // הכנסה כ-DBNull בטבלה
                    }
                    else if (value.IsInt32) // אם הערך הוא מספר שלם 32 ביט
                    {
                        row[key] = value.AsInt32;
                    }
                    else if (value.IsInt64) // אם הערך הוא מספר שלם 64 ביט
                    {
                        row[key] = value.AsInt64;
                    }
                    else if (value.IsDouble) // אם הערך הוא מספר Double
                    {
                        row[key] = value.AsDouble;
                    }
                    else if (value.IsDecimal128) // אם הערך הוא מספר Decimal128 (MongoDB)
                    {
                        row[key] = (decimal)value.AsDecimal128;
                    }
                    else if (value.IsBoolean) // אם הערך הוא בוליאני (true/false)
                    {
                        row[key] = value.AsBoolean;
                    }
                    else if (value.IsValidDateTime) // אם הערך הוא תאריך
                    {
                        row[key] = value.ToUniversalTime(); // המרה לזמן אוניברסלי (UTC)
                    }
                    else if (value.IsObjectId) // אם הערך הוא ObjectId (MongoDB מזהה ייחודי)
                    {
                        row[key] = value.AsObjectId.ToString(); // המרה למחרוזת
                    }
                    else
                    {
                        row[key] = value.ToString(); // המרה למחרוזת עבור כל סוג אחר
                    }
                }

                table.Rows.Add(row); // הוספת השורה לטבלה
            }

            return table; // החזרת אובייקט DataTable עם הנתונים שהומרו
        }

        private void SubmitOrderDetailsBtn_Click(object sender, EventArgs e) // פונקציה לשליחת פרטי ההזמנה למסד הנתונים
        {
            try
            {
                // 📌 בדיקה שכל השדות מולאו
                if (string.IsNullOrWhiteSpace(FirstName.Text) ||
                    string.IsNullOrWhiteSpace(LastName.Text) ||
                    string.IsNullOrWhiteSpace(PhoneNumber.Text) ||
                    string.IsNullOrWhiteSpace(Adress.Text) ||
                    string.IsNullOrWhiteSpace(EmailBox.Text) ||
                    BikeTypeBox.SelectedItem == null ||
                    BikeSizeBox.SelectedItem == null ||
                    BikeColorBox.SelectedItem == null)
                {
                    MessageBox.Show("Please fill all the required fields before proceeding.");
                    return;
                }

                // 📌 איסוף נתוני הלקוח
                string firstName = FirstName.Text.Trim();
                string lastName = LastName.Text.Trim();
                string phoneNumber = NormalizePhoneNumber(PhoneNumber.Text.Trim());
                string address = Adress.Text.Trim();
                string email = EmailBox.Text.Trim();
                string bikeType = BikeTypeBox.SelectedItem.ToString().Trim();
                string bikeSize = BikeSizeBox.SelectedItem.ToString().Trim();
                string bikeColor = BikeColorBox.SelectedItem.ToString().Trim();
                int quantity = (int)QuantitySelector.Value;

                // 📌 בדיקת מלאי זמין
                int currentStock = GetBikeStock(bikeType, bikeSize, bikeColor);
                Console.WriteLine($"🔍 Current Stock: {currentStock}, Requested: {quantity}");

                if (currentStock < quantity)
                {
                    MessageBox.Show($"Insufficient stock. Available: {currentStock}, Requested: {quantity}");
                    return;
                }

                // 📌 בדיקה אם הלקוח קיים
                object customerId = EnsureCustomerExists(firstName, lastName, phoneNumber);
                if (customerId == null) // אם הלקוח לא קיים, מוסיפים אותו
                {
                    customerId = AddNewCustomer(firstName, lastName, phoneNumber, address, email);
                    if (customerId == null) // אם הוספת הלקוח נכשלה
                    {
                        MessageBox.Show("Failed to add new customer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 📌 שליפת מזהה האופניים מהמאגר
                int? bikeIDNullable = GetBikeID(bikeType, bikeSize, bikeColor);
                if (bikeIDNullable == null)
                {
                    MessageBox.Show("❌ Error: Bike ID not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int bikeID = bikeIDNullable.Value;
                int newOrderId = -1;
                decimal unitPrice = GetBikePrice(bikeType, bikeSize, bikeColor);
                decimal totalPrice = quantity * unitPrice;

                if (_selectedDatabase == "sql") // 📌 **SQL Mode**
                {
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        conn.Open();

                        // 📌 יצירת הזמנה חדשה
                        string insertOrderQuery = @"
            INSERT INTO Orders (CustomerID, OrderDate, TotalAmount)
            OUTPUT INSERTED.OrderID
            VALUES (@CustomerID, GETDATE(), @TotalAmount)";

                        using (SqlCommand cmd = new SqlCommand(insertOrderQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@CustomerID", customerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TotalAmount", totalPrice);

                            var result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                newOrderId = Convert.ToInt32(result);
                            }
                        }

                        if (newOrderId == -1) // אם הוספת ההזמנה נכשלה
                        {
                            MessageBox.Show("Failed to create new order.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // 📌 הוספת פרטי ההזמנה לטבלת OrderDetails
                        string insertOrderDetailsQuery = @"
            INSERT INTO OrderDetails (OrderID, BikeID, Quantity, UnitPrice)
            VALUES (@OrderID, @BikeID, @Quantity, @UnitPrice)";

                        using (SqlCommand cmd = new SqlCommand(insertOrderDetailsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", newOrderId);
                            cmd.Parameters.AddWithValue("@BikeID", bikeID);
                            cmd.Parameters.AddWithValue("@Quantity", quantity);
                            cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                            cmd.ExecuteNonQuery();
                        }

                        // ✅ עדכון המלאי ב-SQL
                        Console.WriteLine("🔄 Updating SQL stock...");
                        UpdateBikeStockSQL(bikeID, quantity);
                    }
                }
                else if (_selectedDatabase == "mongo") // 📌 **MongoDB Mode**
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders");

                    var newOrder = new BsonDocument
        {
            { "CustomerID", new ObjectId(customerId.ToString()) },
            { "OrderDate", DateTime.UtcNow },
            { "TotalAmount", totalPrice },
            { "OrderDetails", new BsonArray
                {
                    new BsonDocument
                    {
                        { "BikeType", bikeType },
                        { "BikeSize", bikeSize },
                        { "BikeColor", bikeColor },
                        { "Quantity", quantity },
                        { "UnitPrice", unitPrice }
                    }
                }
            }
        };

                    ordersCollection.InsertOne(newOrder);
                    newOrderId = newOrder["_id"].AsObjectId.Timestamp;

                    // ✅ עדכון מלאי ב-MongoDB עם הדפסה לבדיקת כמות נכונה
                    Console.WriteLine("🔄 Updating MongoDB stock...");
                    UpdateBikeStockMongo(bikeType, bikeSize, bikeColor, -quantity);
                }

                // ✅ טעינת פרטי ההזמנה לגריד
                if (_selectedDatabase == "sql" && newOrderId != -1)
                {
                    string query = @"
        SELECT o.OrderID, c.CustomerID, c.FirstName, c.LastName, 
               b.Type AS BikeType, b.BikeSize, b.Color, 
               od.Quantity, od.UnitPrice, (od.Quantity * od.UnitPrice) AS TotalPrice
        FROM Orders o
        INNER JOIN OrderDetails od ON o.OrderID = od.OrderID
        INNER JOIN Customers c ON o.CustomerID = c.CustomerID
        INNER JOIN BikeTypes b ON od.BikeID = b.BikeID
        WHERE o.OrderID = @OrderID";

                    LoadDataToGrid(query, new Dictionary<string, object> { { "@OrderID", newOrderId } });
                }
                else if (_selectedDatabase == "mongo")
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders");
                    var lastOrder = ordersCollection.Find(new BsonDocument()).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(1).ToList();
                    if (lastOrder.Count > 0)
                    {
                        var table = ConvertBsonToDataTable(lastOrder);
                        dataGridViewMain.DataSource = table;
                    }
                }

                MessageBox.Show("Order submitted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //פונקציות לצפיה בנתוני הזמנות פר לקוח ויומי ושנתי
        private void ShowCusOrderHistoryBtn_Click(object sender, EventArgs e) // פונקציה להצגת היסטוריית ההזמנות של לקוח
        {
            try
            {
                // 📌 בקשת קלט מהמשתמש
                string input = PromptForInput("Enter Customer ID or Phone Number:", "Customer Search"); // חלונית קלט לקבלת מזהה הלקוח או מספר הטלפון
                if (string.IsNullOrWhiteSpace(input)) // בדיקה אם הקלט ריק
                {
                    MessageBox.Show("Customer ID or Phone Number is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 📌 נרמול מספר הטלפון אם מדובר במספר טלפון
                string normalizedPhoneNumber = NormalizePhoneNumber(input.Trim());
                bool isPhoneNumber = normalizedPhoneNumber.Length >= 9 && normalizedPhoneNumber.Length <= 10 && long.TryParse(normalizedPhoneNumber, out _); // בדיקה אם הקלט הוא מספר טלפון

                // 📌 בדיקה האם מדובר ב-ID (מספר בלבד) אך לא במספר טלפון
                bool isNumeric = int.TryParse(input, out int customerId) && !isPhoneNumber;

                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    string query = @"
            SELECT 
                o.OrderID AS 'Order ID',
                o.OrderDate AS 'Order Date',
                o.TotalAmount AS 'Total Amount',
                c.FirstName AS 'First Name',
                c.LastName AS 'Last Name',
                c.Email AS 'Email',
                REPLACE(REPLACE(REPLACE(c.PhoneNumber, '-', ''), ' ', ''), '(', '') AS 'Phone Number',
                c.Address AS 'Address',
                bt.Type AS 'Bike Type',
                bt.BikeSize AS 'Bike Size',
                bt.Color AS 'Bike Color',
                od.Quantity AS 'Quantity',
                od.UnitPrice AS 'Unit Price',
                od.TotalPrice AS 'Total Price'
            FROM 
                Orders o
            JOIN 
                Customers c ON o.CustomerID = c.CustomerID
            JOIN 
                OrderDetails od ON o.OrderID = od.OrderID
            JOIN 
                BikeTypes bt ON od.BikeID = bt.BikeID
            WHERE 
                (@CustomerID > 0 AND c.CustomerID = @CustomerID) OR
                (@PhoneNumber IS NOT NULL AND REPLACE(REPLACE(REPLACE(c.PhoneNumber, '-', ''), ' ', ''), '(', '') = @PhoneNumber)
            ORDER BY 
                o.OrderDate DESC"; // שאילתא להיסטוריית ההזמנות לפי מספר טלפון או מזהה לקוח

                    Dictionary<string, object> parameters = new();
                    if (isNumeric) // אם הקלט הוא מזהה לקוח
                    {
                        parameters["@CustomerID"] = customerId;
                        parameters["@PhoneNumber"] = DBNull.Value;
                    }
                    else if (isPhoneNumber) // אם הקלט הוא מספר טלפון
                    {
                        parameters["@CustomerID"] = DBNull.Value;
                        parameters["@PhoneNumber"] = normalizedPhoneNumber;
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter a valid Customer ID or Phone Number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    LoadDataToGrid(query, parameters); // שליחת הבקשה וטעינת הנתונים לטבלה
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו
                    var customersCollection = _mongoHelper.GetCollection<BsonDocument>("Customers"); // קבלת אוסף הלקוחות ממונגו

                    BsonDocument customerFilter = new BsonDocument();
                    if (isNumeric) // אם הקלט הוא מזהה לקוח
                    {
                        customerFilter = new BsonDocument { { "_id", new ObjectId(input) } };
                    }
                    else if (isPhoneNumber) // אם הקלט הוא מספר טלפון
                    {
                        customerFilter = new BsonDocument { { "PhoneNumber", normalizedPhoneNumber } };
                    }
                    else
                    {
                        MessageBox.Show("Invalid input. Please enter a valid Customer ID or Phone Number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var customer = customersCollection.Find(customerFilter).FirstOrDefault(); // חיפוש הלקוח במסד הנתונים
                    if (customer == null) // אם הלקוח לא נמצא
                    {
                        MessageBox.Show("Customer not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var orderFilter = Builders<BsonDocument>.Filter.Eq("CustomerID", customer["_id"]); // יצירת פילטר להזמנות של הלקוח
                    var orders = ordersCollection.Find(orderFilter).Sort(Builders<BsonDocument>.Sort.Descending("OrderDate")).ToList(); // שליפת ההזמנות מהמסד

                    if (orders.Count == 0) // אם אין הזמנות ללקוח
                    {
                        MessageBox.Show("No orders found for this customer.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    DataTable table = ConvertBsonToDataTable(orders); // המרת הנתונים לטבלה
                    dataGridViewMain.DataSource = table; // הצגת הנתונים ב-DataGridView
                }
            }
            catch (Exception ex) // טיפול בשגיאות
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void inventoryBtn_Click(object sender, EventArgs e) // פונקציה להצגת המלאי של האופניים
        {
            try
            {
                if (_selectedDatabase == "sql") // אם מסד הנתונים שנבחר הוא SQL
                {
                    string query = @"
            SELECT 
                BikeID AS 'Bike ID', 
                Type AS 'Bike Type', 
                BikeSize AS 'Bike Size', 
                Color AS 'Bike Color', 
                StockQuantity AS 'Stock Quantity', 
                SalePrice AS 'Sale Price'
            FROM BikeTypes
            ORDER BY BikeID ASC"; // שאילתת SQL לשליפת נתוני המלאי מהטבלה BikeTypes

                    LoadDataToGrid(query); // טעינת הנתונים לטבלה ב-DataGridView
                }
                else if (_selectedDatabase == "mongo") // אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var bikesCollection = _mongoHelper.GetCollection<BsonDocument>("BikeTypes"); // קבלת אוסף האופניים ממונגו
                    var bikes = bikesCollection.Find(FilterDefinition<BsonDocument>.Empty).ToList(); // שליפת כל הנתונים מהאוסף

                    if (bikes.Count == 0) // אם אין מלאי במסד הנתונים
                    {
                        MessageBox.Show("No bike inventory found in MongoDB.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    DataTable table = ConvertBsonToDataTable(bikes); // המרת הנתונים לטבלת DataTable
                    dataGridViewMain.DataSource = table; // הצגת הנתונים בטבלת המשתמש
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBikeStockSQL(int bikeID, int quantity) // פונקציה לעדכון מלאי האופניים ב-SQL לאחר הזמנה
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                {
                    conn.Open(); // פתיחת החיבור למסד הנתונים

                    // 🟢 ניסיון להוריד מלאי מהמחסן
                    string updateStockQuery = @"
            UPDATE BikeTypes 
            SET StockQuantity = StockQuantity - @Quantity 
            WHERE BikeID = @BikeID AND StockQuantity >= @Quantity"; // שאילתת SQL להורדת כמות מהמלאי אם יש מספיק יחידות

                    using (SqlCommand cmd = new SqlCommand(updateStockQuery, conn)) // יצירת פקודת SQL
                    {
                        cmd.Parameters.AddWithValue("@Quantity", quantity); // הוספת כמות כפרמטר לשאילתה
                        cmd.Parameters.AddWithValue("@BikeID", bikeID); // הוספת מזהה האופניים כפרמטר לשאילתה
                        int rowsAffected = cmd.ExecuteNonQuery(); // ביצוע השאילתה ובדיקת מספר הרשומות שעודכנו

                        if (rowsAffected == 0) // אם לא ניתן לעדכן כי אין מספיק מלאי
                        {
                            MessageBox.Show("⚠️ There is not enough stock to complete the order.\nPlease reduce the quantity or try again later.",
                                            "Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // 🟡 בדיקה אם הטריגר הפעיל הזמנת אספקה
                    string checkTriggerQuery = @"
            SELECT COUNT(*) FROM SupplyTransactions 
            WHERE BikeID = @BikeID AND SupplyDate >= DATEADD(SECOND, -10, GETDATE())";  // ✅ החלפת OrderDate ב-SupplyDate כדי לבדוק אם נוצרה הזמנה חדשה לאחר עדכון המלאי

                    using (SqlCommand checkCmd = new SqlCommand(checkTriggerQuery, conn)) // יצירת פקודת SQL לבדיקה אם טריגר יצר הזמנת אספקה
                    {
                        checkCmd.Parameters.AddWithValue("@BikeID", bikeID); // הוספת מזהה האופניים כפרמטר
                        int supplyOrders = Convert.ToInt32(checkCmd.ExecuteScalar()); // בדיקת מספר ההזמנות החדשות שנוצרו

                        if (supplyOrders > 0) // אם נוצרה הזמנת אספקה חדשה בעקבות הטריגר
                        {
                            MessageBox.Show("📦 Automatic supply order has been placed!\nA new stock shipment will arrive soon.",
                                            "Stock Replenishment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else // אם לא נדרשה הזמנת אספקה
                        {
                            MessageBox.Show("✅ Order processed successfully!\nStock has been updated.",
                                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"❌ An error occurred while updating stock:\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBikeStockMongo(string bikeType, string bikeSize, string bikeColor, int quantity) // פונקציה לעדכון מלאי האופניים ב-MongoDB
        {
            try
            {
                var collection = _mongoHelper.GetCollection<BsonDocument>("BikeTypes"); // קבלת אוסף האופניים ממונגו

                // ✨ חיתוך רווחים מיותרים והתאמת השמות
                bikeType = bikeType.Trim(); // הסרת רווחים מיותרים מסוג האופניים
                bikeSize = bikeSize.Trim(); // הסרת רווחים מיותרים מגודל האופניים
                bikeColor = bikeColor.Trim(); // הסרת רווחים מיותרים מצבע האופניים

                // ✅ הדפסת נתוני DEBUG לבדיקה
                Console.WriteLine($"🔍 Attempting to update stock in MongoDB...");
                Console.WriteLine($"Searching for -> Type: '{bikeType}', Size: '{bikeSize}', Color: '{bikeColor}', Quantity Change: {quantity}");

                var filter = Builders<BsonDocument>.Filter.And( // יצירת פילטר למציאת האופניים במסד הנתונים
                    Builders<BsonDocument>.Filter.Regex("Type", new BsonRegularExpression(bikeType, "i")),  // חיפוש לפי דמיון (לא תלוי אותיות גדולות/קטנות)
                    Builders<BsonDocument>.Filter.Regex("BikeSize", new BsonRegularExpression(bikeSize, "i")), // התאמה לגודל עם רגישות לרווחים ואותיות
                    Builders<BsonDocument>.Filter.Eq("Color", bikeColor)  // התאמה מדויקת לצבע
                );

                var update = Builders<BsonDocument>.Update.Inc("StockQuantity", -quantity); // עדכון מלאי על ידי הורדה של הכמות המבוקשת

                var result = collection.UpdateOne(filter, update); // ביצוע העדכון במסד הנתונים

                // ✅ הצגת תוצאות כדי לבדוק אם הרשומה עודכנה
                Console.WriteLine($"MatchedCount: {result.MatchedCount}, ModifiedCount: {result.ModifiedCount}");

                if (result.MatchedCount == 0) // אם לא נמצאה רשומה תואמת במלאי
                {
                    MessageBox.Show($"❗ No matching bike found in MongoDB for update:\nType: {bikeType}, Size: {bikeSize}, Color: {bikeColor}.",
                                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (result.ModifiedCount == 0) // אם נמצאה רשומה אך לא עודכנה
                {
                    MessageBox.Show($"⚠️ Bike found, but stock was not modified. Please check values manually.",
                                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של כשל
            {
                MessageBox.Show($"❌ Error updating stock in MongoDB: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SalesPerDayBtn_Click(object sender, EventArgs e) // פונקציה להצגת המכירות לפי יום
        {
            try
            {
                // 📌 קבלת תאריך נבחר מה-DateTimePicker
                DateTime selectedDate = SalesDatePicker.Value.Date; // שליפת התאריך הנבחר ללא שעות

                if (_selectedDatabase == "sql") // 📌 אם מסד הנתונים שנבחר הוא SQL
                {
                    using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                    {
                        conn.Open(); // פתיחת החיבור

                        // 📌 בדיקה אם קיימות הזמנות ליום הנבחר
                        string countQuery = "SELECT COUNT(*) FROM Orders WHERE CAST(OrderDate AS DATE) = @SelectedDate";

                        using (SqlCommand cmd = new SqlCommand(countQuery, conn)) // יצירת פקודת SQL
                        {
                            cmd.Parameters.AddWithValue("@SelectedDate", selectedDate); // הוספת התאריך כפרמטר
                            int orderCount = (int)cmd.ExecuteScalar(); // שליפת מספר ההזמנות ליום זה

                            if (orderCount == 0) // אם לא נמצאו מכירות ליום הנבחר
                            {
                                MessageBox.Show($"No sales found for {selectedDate.ToShortDateString()}.", "No Sales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }

                        // 📌 שאילתה לשליפת פרטי המכירות ליום הנבחר
                        string query = @"
                SELECT 
                    o.OrderID AS 'Order ID',
                    c.FirstName AS 'Customer First Name',
                    c.LastName AS 'Customer Last Name',
                    o.OrderDate AS 'Order Date',
                    od.Quantity AS 'Quantity',
                    bt.Type AS 'Bike Type',
                    bt.BikeSize AS 'Bike Size',
                    bt.Color AS 'Bike Color',
                    od.UnitPrice AS 'Unit Price',
                    od.Quantity * od.UnitPrice AS 'Total Price'
                FROM 
                    Orders o
                JOIN 
                    Customers c ON o.CustomerID = c.CustomerID
                JOIN 
                    OrderDetails od ON o.OrderID = od.OrderID
                JOIN 
                    BikeTypes bt ON od.BikeID = bt.BikeID
                WHERE 
                    CAST(o.OrderDate AS DATE) = @SelectedDate
                ORDER BY 
                    o.OrderDate DESC"; // שליפת פרטי ההזמנות לאותו יום עם סידור לפי התאריך מהחדש לישן

                        Dictionary<string, object> parameters = new()
                        {
                            { "@SelectedDate", selectedDate }
                        };

                        // 📌 טעינת הנתונים ל-DataGridView
                        LoadDataToGrid(query, parameters);
                    }
                }
                else if (_selectedDatabase == "mongo") // 📌 אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו

                    // 📌 סינון לפי תאריך
                    var filter = Builders<BsonDocument>.Filter.Gte("OrderDate", selectedDate) & // הזמנות מתאריך זה ומעלה
                                 Builders<BsonDocument>.Filter.Lt("OrderDate", selectedDate.AddDays(1)); // הזמנות לפני יום המחרת

                    var salesData = ordersCollection.Find(filter).ToList(); // שליפת הנתונים מהמסד

                    if (salesData.Count == 0) // אם לא נמצאו מכירות ליום הנבחר
                    {
                        MessageBox.Show($"No sales found for {selectedDate.ToShortDateString()}.", "No Sales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // 📌 המרת הנתונים לטבלת DataTable ושימוש ב-LoadDataToGrid
                    DataTable table = ConvertBsonToDataTable(salesData);
                    dataGridViewMain.DataSource = table; // הצגת הנתונים ב-DataGridView
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowYearlySalesBtn_Click(object sender, EventArgs e) // פונקציה להצגת מכירות שנתיות
        {
            try
            {
                // 📌 בדיקה אם נבחרה שנה ב-ComboBox
                if (YearComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a year.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 📌 קבלת השנה שנבחרה מתוך ה-ComboBox
                string selectedYear = YearComboBox.SelectedItem.ToString();

                if (_selectedDatabase == "sql") // 📌 אם מסד הנתונים שנבחר הוא SQL
                {
                    string query = @"
            SELECT 
                o.OrderID AS 'Order ID',
                o.OrderDate AS 'Order Date',
                o.TotalAmount AS 'Total Amount',
                c.FirstName AS 'Customer First Name',
                c.LastName AS 'Customer Last Name'
            FROM 
                Orders o
            JOIN 
                Customers c ON o.CustomerID = c.CustomerID
            WHERE 
                YEAR(o.OrderDate) = @SelectedYear
            ORDER BY 
                o.OrderDate"; // שליפת כל ההזמנות לפי השנה שנבחרה עם מיון לפי תאריך ההזמנה

                    Dictionary<string, object> parameters = new()
                    {
                        { "@SelectedYear", selectedYear }
                    };

                    // 📌 טעינת הנתונים ל-DataGridView
                    LoadDataToGrid(query, parameters);
                }
                else if (_selectedDatabase == "mongo") // 📌 אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו

                    // 📌 חישוב טווח תאריכים לתחילת וסוף השנה שנבחרה
                    DateTime startOfYear = new DateTime(int.Parse(selectedYear), 1, 1); // תחילת השנה
                    DateTime endOfYear = new DateTime(int.Parse(selectedYear), 12, 31, 23, 59, 59); // סוף השנה

                    var filter = Builders<BsonDocument>.Filter.Gte("OrderDate", startOfYear) & // הזמנות מתאריך זה ומעלה
                                 Builders<BsonDocument>.Filter.Lte("OrderDate", endOfYear); // הזמנות עד סוף השנה

                    var yearlySales = ordersCollection.Find(filter).ToList(); // שליפת כל ההזמנות מהמסד

                    if (yearlySales.Count == 0) // אם אין מכירות לשנה שנבחרה
                    {
                        MessageBox.Show($"No sales found for the year {selectedYear}.", "No Sales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // 📌 המרת הנתונים ל-DataTable ושימוש ב-LoadDataToGrid
                    DataTable table = ConvertBsonToDataTable(yearlySales);
                    dataGridViewMain.DataSource = table; // הצגת הנתונים ב-DataGridView
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // פונקציה להצגת הודעת קלט למשתמש
        private string PromptForInput(string message, string title) // פונקציה להצגת חלונית קלט למשתמש
        {
            return Microsoft.VisualBasic.Interaction.InputBox(message, title, ""); // הצגת חלונית קלט עם הודעה וכותרת
        }

        // פונקציה לנרמול מספר טלפון
        private string NormalizePhoneNumber(string phoneNumber) // פונקציה לנרמול מספר טלפון – השארת ספרות בלבד
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) // בדיקה אם מספר הטלפון ריק או מכיל רק רווחים
            {
                return string.Empty; // החזרת מחרוזת ריקה אם הקלט אינו תקף
            }

            // 📌 השארת ספרות בלבד על ידי סינון כל התווים שאינם ספרות
            string normalized = new string(phoneNumber.Where(char.IsDigit).ToArray());

            return normalized; // החזרת מספר הטלפון לאחר נרמול
        }

        private decimal GetBikePrice(string bikeType, string bikeSize, string bikeColor) // פונקציה לקבלת מחיר האופניים לפי סוג, גודל וצבע
        {
            try
            {
                if (_selectedDatabase == "sql") // 📌 אם מסד הנתונים שנבחר הוא SQL
                {
                    if (conn == null) // אם החיבור למסד הנתונים אינו מאותחל
                    {
                        conn = new SqlConnection(connStr); // יצירת חיבור חדש
                    }
                    if (conn.State != ConnectionState.Open) // אם החיבור סגור
                    {
                        conn.Open(); // פתיחת החיבור למסד הנתונים
                    }

                    // 📌 שאילתא לשליפת מחיר המכירה מהטבלה BikeTypes לפי סוג, גודל וצבע
                    string query = "SELECT SalePrice FROM BikeTypes WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color";

                    using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                    {
                        cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים כפרמטר
                        cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים כפרמטר

                        object result = cmd.ExecuteScalar(); // ביצוע השאילתה והחזרת תוצאה יחידה

                        return (result != null && decimal.TryParse(result.ToString(), out decimal price)) ? price : 0; // המרת התוצאה למחיר, ואם לא תקף - החזרת 0
                    }
                }
                else if (_selectedDatabase == "mongo") // 📌 אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var filter = Builders<BsonDocument>.Filter.And( // יצירת פילטר למציאת רשומת האופניים לפי סוג, גודל וצבע
                        Builders<BsonDocument>.Filter.Eq("Type", bikeType),
                        Builders<BsonDocument>.Filter.Eq("BikeSize", bikeSize),
                        Builders<BsonDocument>.Filter.Eq("Color", bikeColor)
                    );

                    var collection = _mongoHelper.GetCollection<BsonDocument>("BikeTypes"); // קבלת אוסף האופניים ממונגו
                    var document = collection.Find(filter).FirstOrDefault(); // חיפוש הרשומה הראשונה שתואמת לפילטר

                    return (document != null && document.Contains("SalePrice")) ? document["SalePrice"].ToDecimal() : 0; // המרת המחיר למחיר מספרי, ואם לא נמצא - החזרת 0
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                // יש לרשום ל-Log במקום להציג MessageBox ישירות
                Console.WriteLine($"Error retrieving bike price: {ex.Message}"); // רישום שגיאה ליומן
            }
            return 0; // מחזיר 0 במקרה של שגיאה
        }
        //
        //פונקציות לביצוע הזמנות חדשו או עדכון הזמנות או עדכון מלאי אופניים
        private object AddNewOrder(object customerId, string bikeType, string bikeSize, string bikeColor, int quantity, decimal unitPrice) // פונקציה להוספת הזמנה חדשה
        {
            try
            {
                decimal totalAmount = quantity * unitPrice; // חישוב המחיר הכולל של ההזמנה

                if (_selectedDatabase == "sql") // 📌 אם מסד הנתונים שנבחר הוא SQL
                {
                    using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                    {
                        conn.Open(); // פתיחת החיבור למסד הנתונים

                        // 📌 יצירת ההזמנה בטבלת Orders
                        string insertOrderQuery = @"
                    INSERT INTO Orders (CustomerID, OrderDate, TotalAmount) 
                    OUTPUT INSERTED.OrderID 
                    VALUES (@CustomerID, GETDATE(), @TotalAmount)"; // הכנסת הזמנה חדשה וקבלת המזהה שלה

                        int newOrderId;
                        using (SqlCommand cmd = new SqlCommand(insertOrderQuery, conn)) // יצירת פקודת SQL
                        {
                            cmd.Parameters.AddWithValue("@CustomerID", customerId); // הוספת מזהה הלקוח כפרמטר
                            cmd.Parameters.AddWithValue("@TotalAmount", totalAmount); // הוספת המחיר הכולל כפרמטר
                            newOrderId = Convert.ToInt32(cmd.ExecuteScalar()); // ביצוע השאילתה וקבלת מזהה ההזמנה החדשה
                        }

                        // 📌 הוספת פרטי ההזמנה לטבלת OrderDetails
                        string insertOrderDetailsQuery = @"
                    INSERT INTO OrderDetails (OrderID, BikeID, Quantity, UnitPrice, TotalPrice)
                    VALUES (@OrderID, (SELECT BikeID FROM BikeTypes WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color), 
                            @Quantity, @UnitPrice, @TotalPrice)"; // הכנסת פרטי האופניים שהוזמנו

                        using (SqlCommand cmd = new SqlCommand(insertOrderDetailsQuery, conn)) // יצירת פקודת SQL
                        {
                            cmd.Parameters.AddWithValue("@OrderID", newOrderId); // הוספת מזהה ההזמנה
                            cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים
                            cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים
                            cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים
                            cmd.Parameters.AddWithValue("@Quantity", quantity); // הוספת כמות
                            cmd.Parameters.AddWithValue("@UnitPrice", unitPrice); // הוספת מחיר ליחידה
                            cmd.Parameters.AddWithValue("@TotalPrice", totalAmount); // הוספת מחיר כולל
                            cmd.ExecuteNonQuery(); // ביצוע השאילתה
                        }

                        return newOrderId; // החזרת מזהה ההזמנה
                    }
                }
                else if (_selectedDatabase == "mongo") // 📌 אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו

                    var newOrder = new BsonDocument // יצירת מסמך הזמנה חדש
                    {
                        { "CustomerID", new ObjectId(customerId.ToString()) }, // הוספת מזהה הלקוח
                        { "OrderDate", DateTime.UtcNow }, // הוספת תאריך ביצוע ההזמנה
                        { "TotalAmount", totalAmount }, // הוספת מחיר כולל
                        { "OrderDetails", new BsonArray // יצירת מערך פרטי ההזמנה
                            {
                                new BsonDocument
                                {
                                    { "BikeType", bikeType }, // סוג האופניים
                                    { "BikeSize", bikeSize }, // גודל האופניים
                                    { "BikeColor", bikeColor }, // צבע האופניים
                                    { "Quantity", quantity }, // כמות
                                    { "UnitPrice", unitPrice }, // מחיר ליחידה
                                    { "TotalPrice", totalAmount } // מחיר כולל
                                }
                            }
                        }
                    };

                    ordersCollection.InsertOne(newOrder); // הוספת ההזמנה למסד הנתונים
                    return newOrder["_id"].AsObjectId; // מחזיר את מזהה ההזמנה ממונגו
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error adding order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return -1; // במקרה של שגיאה מוחזר -1
        }

        private void AddOrderDetails(object orderId, string bikeType, string bikeSize, string bikeColor, int quantity, decimal unitPrice) // פונקציה להוספת פרטי הזמנה
        {
            try
            {
                decimal totalPrice = quantity * unitPrice; // חישוב המחיר הכולל של ההזמנה

                if (_selectedDatabase == "sql") // 📌 אם מסד הנתונים שנבחר הוא SQL
                {
                    using (SqlConnection conn = new SqlConnection(connStr)) // יצירת חיבור למסד הנתונים SQL
                    {
                        conn.Open(); // פתיחת החיבור למסד הנתונים

                        // 📌 שאילתת SQL להוספת פרטי הזמנה לטבלת OrderDetails
                        string query = @"
                    INSERT INTO OrderDetails (OrderID, BikeID, Quantity, UnitPrice, TotalPrice)
                    VALUES (@OrderID, (SELECT BikeID FROM BikeTypes WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color), 
                            @Quantity, @UnitPrice, @TotalPrice)";

                        using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                        {
                            cmd.Parameters.AddWithValue("@OrderID", orderId); // הוספת מזהה ההזמנה כפרמטר
                            cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים כפרמטר
                            cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים כפרמטר
                            cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים כפרמטר
                            cmd.Parameters.AddWithValue("@Quantity", quantity); // הוספת כמות האופניים כפרמטר
                            cmd.Parameters.AddWithValue("@UnitPrice", unitPrice); // הוספת מחיר ליחידה כפרמטר
                            cmd.Parameters.AddWithValue("@TotalPrice", totalPrice); // הוספת המחיר הכולל כפרמטר
                            cmd.ExecuteNonQuery(); // ביצוע השאילתה
                        }
                    }
                }
                else if (_selectedDatabase == "mongo") // 📌 אם מסד הנתונים שנבחר הוא MongoDB
                {
                    var ordersCollection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(orderId.ToString())); // יצירת פילטר לאיתור ההזמנה

                    // 📌 עדכון מסמך ההזמנה על ידי הוספת פרטי הזמנה לרשימת OrderDetails
                    var update = Builders<BsonDocument>.Update.Push("OrderDetails", new BsonDocument
                    {
                        { "BikeType", bikeType }, // סוג האופניים
                        { "BikeSize", bikeSize }, // גודל האופניים
                        { "BikeColor", bikeColor }, // צבע האופניים
                        { "Quantity", quantity }, // כמות
                        { "UnitPrice", unitPrice }, // מחיר ליחידה
                        { "TotalPrice", totalPrice } // מחיר כולל
                    });

                    var result = ordersCollection.UpdateOne(filter, update); // ביצוע העדכון במסד הנתונים

                    if (result.ModifiedCount == 0) // אם לא בוצע עדכון כלשהו
                    {
                        MessageBox.Show("Failed to update order in MongoDB!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error adding order details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBikeStock(string bikeType, string bikeSize, string bikeColor, int quantityChange) // פונקציה לעדכון מלאי האופניים
        {
            try
            {
                if (conn == null || conn.State != ConnectionState.Open) // 📌 בדיקה אם החיבור למסד הנתונים סגור או לא מאותחל
                {
                    conn = new SqlConnection(connStr); // יצירת חיבור חדש למסד הנתונים
                    conn.Open(); // פתיחת החיבור
                }

                // 📌 שאילתת SQL לעדכון כמות המלאי בהתאם לסוג, גודל וצבע האופניים
                string query = "UPDATE BikeTypes SET StockQuantity = StockQuantity + @QuantityChange " +
                               "WHERE Type = @Type AND BikeSize = @BikeSize AND Color = @Color";

                using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                {
                    cmd.Parameters.AddWithValue("@QuantityChange", -Math.Abs(quantityChange)); // הפיכת השינוי תמיד לשלילי (הורדה מהמלאי)
                    cmd.Parameters.AddWithValue("@Type", bikeType); // הוספת סוג האופניים כפרמטר
                    cmd.Parameters.AddWithValue("@BikeSize", bikeSize); // הוספת גודל האופניים כפרמטר
                    cmd.Parameters.AddWithValue("@Color", bikeColor); // הוספת צבע האופניים כפרמטר

                    cmd.ExecuteNonQuery(); // ביצוע השאילתה לעדכון המלאי
                }
            }
            catch (Exception ex) // 📌 טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error updating stock: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrderDetailsToGrid(int orderId)
        {
            try
            {
                // שאילתת SQL לשליפת פרטי ההזמנה, כולל מידע על הלקוח, האופניים והתמחור
                string query = @"
        SELECT 
            od.OrderDetailID AS 'Order Detail ID',  -- מזהה שורה בטבלת OrderDetails
            o.OrderID AS 'Order ID',  -- מזהה ההזמנה
            c.FirstName AS 'First Name',  -- שם פרטי של הלקוח
            c.LastName AS 'Last Name',  -- שם משפחה של הלקוח
            REPLACE(REPLACE(REPLACE(c.PhoneNumber, '-', ''), ' ', ''), '(', '') AS 'Phone Number',  -- מספר טלפון מנורמל (ללא רווחים וסימנים)
            c.Email AS 'Email',  -- כתובת מייל של הלקוח
            c.Address AS 'Address',  -- כתובת הלקוח
            bt.Type AS 'Bike Type',  -- סוג האופניים
            bt.BikeSize AS 'Bike Size',  -- גודל האופניים
            bt.Color AS 'Bike Color',  -- צבע האופניים
            od.Quantity AS 'Quantity',  -- כמות האופניים שהוזמנה
            od.UnitPrice AS 'Unit Price',  -- מחיר יחידה
            od.TotalPrice AS 'Total Price'  -- מחיר כולל של השורה
        FROM 
            OrderDetails od
        JOIN 
            Orders o ON od.OrderID = o.OrderID  -- חיבור לטבלת ההזמנות
        JOIN 
            Customers c ON o.CustomerID = c.CustomerID  -- חיבור לטבלת הלקוחות
        JOIN 
            BikeTypes bt ON od.BikeID = bt.BikeID  -- חיבור לטבלת האופניים כדי לקבל מידע נוסף
        WHERE 
            o.OrderID = @OrderID";  // סינון לפי מזהה ההזמנה

                // יצירת מילון פרמטרים להכנסת מזהה ההזמנה
                Dictionary<string, object> parameters = new()
                {
                    { "@OrderID", orderId }  // העברת מזהה ההזמנה כפרמטר
                };

                // טעינת הנתונים לטבלת DataGridView
                LoadDataToGrid(query, parameters);
            }
            catch (Exception ex)
            {
                // טיפול בשגיאות במקרה של תקלה
                MessageBox.Show($"Error loading order details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddTotalRowToGrid() // פונקציה להוספת שורת סיכום לטבלה
        {
            try
            {
                if (dataGridViewMain.DataSource is DataTable dataTable) // בדיקה אם מקור הנתונים הוא DataTable
                {
                    // בדיקה אם קיימת עמודה בשם "Total Price"
                    if (!dataTable.Columns.Contains("Total Price"))
                    {
                        MessageBox.Show("Column 'Total Price' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // מחיקת שורת סיכום קודמת (אם קיימת)
                    var existingTotalRow = dataTable.AsEnumerable()
                        .FirstOrDefault(row => row["Bike Type"] != DBNull.Value && row["Bike Type"].ToString() == "Total");
                    if (existingTotalRow != null)
                    {
                        dataTable.Rows.Remove(existingTotalRow); // הסרת השורה מהטבלה
                    }

                    // חישוב הסכום הכולל של העמודה "Total Price"
                    decimal totalSum = dataTable.AsEnumerable()
                        .Where(row => row["Total Price"] != DBNull.Value && decimal.TryParse(row["Total Price"].ToString(), out _)) // סינון שורות עם ערכים חוקיים
                        .Sum(row => Convert.ToDecimal(row["Total Price"])); // חישוב הסכום הכולל

                    // יצירת שורה חדשה עם סיכום המחיר הכולל
                    DataRow totalRow = dataTable.NewRow();
                    totalRow["Bike Type"] = "Total"; // הכנסת "Total" לכותרת השורה
                    totalRow["Total Price"] = totalSum.ToString("F2"); // הצגת הסכום הכולל בפורמט עם 2 ספרות אחרי הנקודה

                    // הוספת שורת הסכום הכולל לטבלה
                    dataTable.Rows.Add(totalRow);
                }
                else
                {
                    MessageBox.Show("Data source is not a DataTable.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error calculating total: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //פונקציות לטעינת נתונים
        private void LoadDataToGrid(string query, Dictionary<string, object> parameters = null) // פונקציה לטעינת נתונים לגריד (DataGridView)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL עם השאילתה והחיבור למסד הנתונים
                {
                    if (parameters != null) // בדיקה אם הועברו פרמטרים לשאילתה
                    {
                        foreach (var param in parameters) // מעבר על כל הפרמטרים והוספתם לשאילתה
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value); // הוספת כל פרמטר לפקודת SQL
                        }
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd)) // יצירת מתאם נתונים לביצוע השאילתה
                    {
                        DataTable dataTable = new DataTable(); // יצירת אובייקט טבלה
                        adapter.Fill(dataTable); // מילוי הטבלה עם הנתונים מהשאילתה
                        dataGridViewMain.DataSource = dataTable; // הצגת הנתונים בטבלת DataGridView
                    }
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // הצגת הודעת שגיאה למשתמש
            }
        }

        private void LoadYearsToComboBox() // פונקציה לטעינת שנים ל-ComboBox 
        {
            try
            {
                if (_selectedDatabase == "sql") // בדיקה אם מסד הנתונים הוא SQL
                {
                    // שאילתת SQL לשליפת שנים ייחודיות מתוך עמודת OrderDate בטבלת Orders
                    string query = "SELECT DISTINCT YEAR(OrderDate) AS Year FROM Orders ORDER BY Year";

                    using (SqlCommand cmd = new SqlCommand(query, conn)) // יצירת פקודת SQL
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader()) // קריאת הנתונים מהשאילתה
                        {
                            YearComboBox.Items.Clear(); // ניקוי הרשימה לפני הוספת נתונים חדשים
                            while (reader.Read()) // מעבר על כל השורות בתוצאה
                            {
                                string year = reader["Year"].ToString(); // המרת השנה למחרוזת
                                YearComboBox.Items.Add(year); // הוספת השנה לרשימה
                            }
                        }
                    }

                    if (YearComboBox.Items.Count > 0)
                    {
                        YearComboBox.SelectedIndex = 0; // בחירת השנה הראשונה כברירת מחדל
                    }
                }
                else if (_selectedDatabase == "mongo") // בדיקה אם מסד הנתונים הוא MongoDB
                {
                    var collection = _mongoHelper.GetCollection<BsonDocument>("Orders"); // קבלת אוסף ההזמנות ממונגו
                    var documents = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList(); // שליפת כל ההזמנות

                    YearComboBox.Items.Clear(); // ניקוי הרשימה לפני הוספת נתונים חדשים

                    HashSet<int> years = new HashSet<int>(); // יצירת HashSet לשמירת שנים ייחודיות

                    foreach (var doc in documents) // מעבר על כל המסמכים שהתקבלו
                    {
                        if (doc.Contains("OrderDate") && doc["OrderDate"].IsValidDateTime) // בדיקה אם קיים שדה OrderDate והוא תקף
                        {
                            DateTime orderDate = doc["OrderDate"].ToUniversalTime(); // המרת OrderDate לתאריך
                            years.Add(orderDate.Year); // הוספת השנה ל-HashSet (נמנע מכפילויות)
                        }
                    }

                    // הוספת השנים לקומבו בוקס לאחר מיון
                    foreach (int year in years.OrderBy(y => y))
                    {
                        YearComboBox.Items.Add(year.ToString()); // המרת השנה למחרוזת והוספתה ל-ComboBox
                    }

                    if (YearComboBox.Items.Count > 0)
                    {
                        YearComboBox.SelectedIndex = 0; // בחירת השנה הראשונה כברירת מחדל
                    }
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error loading years: {ex.Message}"); // הצגת הודעת שגיאה
            }
        }

        private string SendCommand(string serverIp, int serverPort, Command command) // פונקציה לשליחת פקודה לשרת TCP
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort)) // יצירת חיבור לשרת ב-IP ובפורט שצוינו
                using (NetworkStream stream = client.GetStream()) // יצירת זרם נתונים מול השרת
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true }) // כתיבה לזרם הנתונים עם ניקוי אוטומטי
                using (StreamReader reader = new StreamReader(stream)) // קריאה מזרם הנתונים
                {
                    // המרה של האובייקט Command למחרוזת בפורמט JSON
                    string jsonCommand = JsonConvert.SerializeObject(command);

                    // שליחת ה-JSON לשרת דרך הזרם
                    writer.WriteLine(jsonCommand);

                    // קבלת התגובה מהשרת
                    string response = reader.ReadLine();
                    return response; // החזרת תגובת השרת
                }
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                return $"Error: {ex.Message}"; // החזרת הודעת שגיאה במקרה של כשל
            }
        }

        private void btnSendCommand_Click(object sender, EventArgs e) // פונקציה לשליחת פקודה לשרת בעת לחיצה על הכפתור
        {
            // הגדרת כתובת השרת והפורט
            string serverIp = "127.0.0.1"; // כתובת ה-IP של השרת (localhost)
            int serverPort = 5000;         // הפורט שעליו פועל השרת

            // יצירת אובייקט Command לדוגמה עם פרטי הבקשה
            var command = new Command
            {
                CommandID = 1, // מזהה פקודה
                Action = "insert", // סוג הפעולה (אפשרי: insert, update, delete, select)
                Table = "BikeTypes", // שם הטבלה שעליה יתבצע השינוי
                Parameters = new Dictionary<string, object> // יצירת מילון עם הפרמטרים הרלוונטיים
                {
                    { "Type", "Mountain Bike" },       // סוג האופניים
                    { "Color", "Red" },                // צבע האופניים
                    { "BikeSize", "Medium" },          // גודל האופניים
                    { "StockQuantity", 10 },           // כמות במלאי
                    { "SalePrice", 999.99 },           // מחיר מכירה
                    { "SupplierID", 1 }                // מזהה הספק
                },

                DatabaseClient = "sql" // ציון סוג מסד הנתונים (SQL במקרה זה)
            };

            // שליחת ה-Command לשרת וקבלת תגובה
            string response = SendCommand(serverIp, serverPort, command);

            // הצגת התגובה שהתקבלה מהשרת בתיבת הודעה
            MessageBox.Show(response, "Server Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSelectDatabase_Click(object sender, EventArgs e) // פונקציה לבחירת בסיס הנתונים (SQL או MongoDB)
        {
            var result = MessageBox.Show(
                "Do you want to use MongoDB? Click 'Yes' for MongoDB or 'No' for SQL.", // הודעה למשתמש
                "Select Database", // כותרת ההודעה
                MessageBoxButtons.YesNo, // כפתורי אישור ודחייה
                MessageBoxIcon.Question // הצגת אייקון של שאלה
            );

            string previousDatabase = _selectedDatabase; // שמירת סוג מסד הנתונים הקודם

            // שינוי סוג מסד הנתונים בהתאם לבחירת המשתמש
            if (result == DialogResult.Yes && _selectedDatabase != "mongo") // אם המשתמש בחר MongoDB
            {
                _selectedDatabase = "mongo";
                MessageBox.Show("Switched to MongoDB.");
            }
            else if (result == DialogResult.No && _selectedDatabase != "sql") // אם המשתמש בחר SQL
            {
                _selectedDatabase = "sql";
                MessageBox.Show("Switched to SQL.");
            }
            else
            {
                return; // אם לא היה שינוי, אין צורך להמשיך
            }

            // אם בסיס הנתונים שונה, יש צורך לסגור את החיבורים ולהתחבר מחדש
            try
            {
                if (previousDatabase == "sql" && conn != null && conn.State == ConnectionState.Open) // אם היינו ב-SQL, סוגרים חיבור קודם
                {
                    conn.Close();
                }

                if (_selectedDatabase == "sql") // התחברות למסד הנתונים SQL
                {
                    conn = new SqlConnection(connStr);
                    conn.Open();
                }
                else if (_selectedDatabase == "mongo") // התחברות למסד הנתונים MongoDB
                {
                    _mongoHelper = new MongoDBHelper(mongoConnectionString, mongoDatabaseName);
                }

                // טעינת נתונים מחדש לאחר שינוי בסיס הנתונים
                LoadBikeData(); // טעינת נתוני האופניים
                LoadYearsToComboBox(); // טעינת רשימת השנים
            }
            catch (Exception ex) // טיפול בשגיאות במקרה של תקלה
            {
                MessageBox.Show($"Error switching database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetPlaceholders() // פונקציה לקביעת טקסט ברירת מחדל (Placeholder) עבור שדות הקלט
        {
            // הגדרת טקסט ברירת מחדל עבור כל שדה קלט בטופס
            SetPlaceholder(FirstName, "First Name"); // שם פרטי
            SetPlaceholder(LastName, "Last Name"); // שם משפחה
            SetPlaceholder(PhoneNumber, "Phone Number"); // מספר טלפון
            SetPlaceholder(Adress, "Address"); // כתובת
            SetPlaceholder(EmailBox, "Email"); // כתובת אימייל
        }

        private void SetPlaceholder(TextBox textBox, string placeholderText) // פונקציה להגדרת טקסט placeholder עבור תיבת טקסט
        {
            textBox.Text = placeholderText; // קביעת הטקסט הראשוני כשדה ברירת מחדל
            textBox.ForeColor = Color.Gray; // הגדרת צבע טקסט לאפור כדי להמחיש שהוא placeholder

            textBox.Enter += (sender, e) => // אירוע כניסה לשדה (כאשר המשתמש לוחץ על השדה)
            {
                if (textBox.Text == placeholderText) // אם התוכן הוא ה-placeholder
                {
                    textBox.Text = ""; // מחיקת הטקסט
                    textBox.ForeColor = Color.Black; // שינוי צבע הטקסט לשחור
                }
            };

            textBox.Leave += (sender, e) => // אירוע יציאה מהשדה (כאשר המשתמש עוזב את השדה)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text)) // אם המשתמש לא הכניס תוכן כלשהו
                {
                    textBox.Text = placeholderText; // שחזור ה-placeholder
                    textBox.ForeColor = Color.Gray; // שינוי צבע הטקסט חזרה לאפור
                }
            };
        }

        // פונקציות אירועים שאינן מכילות קוד כרגע (משמשות לטיפול באירועים של רכיבי ממשק המשתמש)

        private void BikeTypeBox_SelectedIndexChanged(object sender, EventArgs e) { } // שינוי סוג האופניים שנבחר
        private void BikeSizeBox_SelectedIndexChanged(object sender, EventArgs e) { } // שינוי גודל האופניים שנבחר
        private void BikeColorBox_SelectedIndexChanged(object sender, EventArgs e) { } // שינוי צבע האופניים שנבחר
        private void Form1_Load(object sender, EventArgs e) { } // טעינת הטופס בעת הפעלת התוכנית
        private void label2_Click(object sender, EventArgs e) { } // לחיצה על תווית מספר 2
        private void label3_Click(object sender, EventArgs e) { } // לחיצה על תווית מספר 3
        private void label4_Click(object sender, EventArgs e) { } // לחיצה על תווית מספר 4
        private void FirstName_TextChanged(object sender, EventArgs e) { } // שינוי טקסט בשדה שם פרטי
        private void LastName_TextChanged(object sender, EventArgs e) { } // שינוי טקסט בשדה שם משפחה
        private void Adress_TextChanged(object sender, EventArgs e) { } // שינוי טקסט בשדה כתובת
        private void EmailBox_TextChanged(object sender, EventArgs e) { } // שינוי טקסט בשדה אימייל
        private void PhoneNumber_TextChanged(object sender, EventArgs e) { } // שינוי טקסט בשדה טלפון
        private void label5_Click_1(object sender, EventArgs e) { } // לחיצה על תווית מספר 5
        private void dataGridViewMain_CellContentClick(object sender, DataGridViewCellEventArgs e) { } // לחיצה על תא בטבלת הנתונים
        private void QuantityLabel_Click(object sender, EventArgs e) { } // לחיצה על תווית כמות
        private void QuantitySelector_ValueChanged(object sender, EventArgs e) { } // שינוי ערך בחירה של כמות
        private void label6_Click(object sender, EventArgs e) { } // לחיצה על תווית מספר 6
        private void YearComboBox_SelectedIndexChanged_1(object sender, EventArgs e) { } // שינוי שנה שנבחרה ב-ComboBox
        private void SalesDatePicker_ValueChanged(object sender, EventArgs e) { } // שינוי תאריך נבחר ב-DatePicker
        private void BikeDetailsPanel_Paint(object sender, PaintEventArgs e) { } // ציור רכיב הפאנל BikeDetails
        private void CustomersInfoPanel_Paint(object sender, PaintEventArgs e) { } // ציור רכיב הפאנל CustomersInfo
        private void SubmitsPanel_Paint(object sender, PaintEventArgs e) { } // ציור רכיב הפאנל Submits


    }

}

