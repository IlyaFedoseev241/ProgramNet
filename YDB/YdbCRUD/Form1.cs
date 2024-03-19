using System;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using Ydb.Sdk;
using Ydb.Sdk.Auth;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace OnlineStore
{
    public partial class Form1 : Form
    {

        private string id = "";
        private int intRow = 0;
        private string nameTable = "Покупатели";
        private string connectNameTable = "Customers";
        private Driver driver;
        private DataTable dt;

        public Form1()
        {
            InitializeComponent();
            resetMe();
            InitDB();

        }

        private async Task InitDB()
        {
            var endpoint = "grpcs://ydb.serverless.yandexcloud.net:2135";
            var database = "/ru-central1/b1gq23dv2rt03suikvnh/etnibs5qqa140dsce2va";
            var token = "t1.9euelZqLjJ2ey4_LjZ6PzMmVi8qWxu3rnpWaloqbjZiNiovJlJyMyMebm8zl8_cpGh5Q-e83a3ge_t3z92lIG1D57zdreB7-zef1656Vmp6Yls2MioqTz5aezZGLlcqL7_zN5_XrnpWajczOi8zKnYvKkZDPzcbIy43v_cXrnpWanpiWzYyKipPPlp7NkYuVyos.I8IVZbeeiTGE4ASA9lABle-SFJxJWrxMWJsx8JMfzaZqSm5vYAGrVw2LPG3wQ_GQIPZGariMgE-YCbdQeCR9DQ";

            var config = new DriverConfig(
                endpoint: endpoint,
                database: database,
                credentials: new TokenProvider(token)
            );

            driver = new Driver(
                config: config
            );

            await driver.Initialize();
            loadData();
        }

        private void resetMe()
        {
            id = string.Empty;

            firstNameTextBox.Text = "";
            lastNameTextBox.Text = "";
            middleNameTextBox.Text = "";
            nameTextBox.Text = "";
            PhoneTxt.Text = "";
            priceTextBox.Value = 0;


            customerUpdateButton.Text = "Изменить ()";
            customerDeleteButton.Text = "Удалить ()";
            productUpdateButton.Text = "Изменить ()";
            productDeleteButton.Text = "Удалить ()";
        }

        private async Task loadData()
        {

            var tableClient = new TableClient(driver, new TableClientConfig());

            switch (nameTable)
            {
                case "Покупатели":
                    connectNameTable = "Customers";
                    break;
                case "Товары":
                    connectNameTable = "Products";
                    break;
                case "Заказы":
                    connectNameTable = "Orders";
                    break;
                case "Заказанные товары":
                    connectNameTable = "OrdersProducts";
                    break;
                default:
                    break;
            }

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM {connectNameTable}";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];

            dt = new DataTable();

            switch (nameTable)
            {
                case "Покупатели":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("Фамилия", typeof(string));
                    dt.Columns.Add("Имя", typeof(string));
                    dt.Columns.Add("Отчество", typeof(string));
                    dt.Columns.Add("Телефон", typeof(string));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["LastName"],
                            (string?)row["FirstName"], (string?)row["MiddleName"], (string?)row["Phone"]);
                    }
                    break;
                case "Товары":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("Название товара", typeof(string));
                    dt.Columns.Add("Цена", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["Name"], (ulong?)row["Price"]);
                    }
                    break;
                case "Заказы":
                    dt.Columns.Add("Id", typeof(ulong));
                    dt.Columns.Add("Состояние", typeof(string));
                    dt.Columns.Add("Общее количество товаров", typeof(ulong));
                    dt.Columns.Add("Цена", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["Id"], (string?)row["Condition"], (ulong?)row["QuantityProduct"],
                            (ulong?)row["Price"]);

                    }
                    break;
                case "Заказанные товары":
                    dt.Columns.Add("ID заказа", typeof(ulong));
                    dt.Columns.Add("ID продукта", typeof(ulong));
                    dt.Columns.Add("Сумма", typeof(ulong));
                    dt.Columns.Add("Количество товаров", typeof(ulong));
                    foreach (var row in resultSet.Rows)
                    {
                        dt.Rows.Add((ulong?)row["OrderId"], (ulong?)row["ProductId"],
                            (ulong?)row["PriceProducts"], (ulong?)row["QuantityProduct"]);
                    }
                    break;
                default:
                    break;
            }

            if (dt.Rows.Count > 0)
            {
                intRow = Convert.ToInt32(dt.Rows.Count.ToString());
            }
            else
            {
                intRow = 0;
            }

            toolStripStatusLabel1.Text = "Number of row(s): " + intRow.ToString();

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }

        private ulong GetLastIdFromTable()
        {
            ulong maxId = 0;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((ulong)dt.Rows[i]["Id"] > maxId)
                    maxId = (ulong)dt.Rows[i]["Id"];
            }
            return maxId + 1;
        }

        private async void customerInsertButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(firstNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(lastNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(middleNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(PhoneTxt.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите ФИО полностью.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $FirstName AS Utf8;
                    DECLARE $LastName AS Utf8;
                    DECLARE $MiddleName AS Utf8;
                    DECLARE $Phone AS Utf8;
                    UPSERT INTO Customers (Id, FirstName, LastName, MiddleName, Phone) VALUES
                        ($Id, $FirstName, $LastName, $MiddleName, $Phone);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(GetLastIdFromTable()) },
                { "$FirstName", YdbValue.MakeUtf8(firstNameTextBox.Text.Trim()) },
                { "$LastName", YdbValue.MakeUtf8(lastNameTextBox.Text.Trim()) },
                { "$MiddleName", YdbValue.MakeUtf8(middleNameTextBox.Text.Trim()) },
                      { "$Phone", YdbValue.MakeUtf8(PhoneTxt.Text.Trim()) },
                        }
                );
            });

            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            loadData();
            resetMe();
        }

        private async void customerUpdateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (string.IsNullOrEmpty(lastNameTextBox.Text.Trim())
                || string.IsNullOrEmpty(firstNameTextBox.Text.Trim()) ||
                string.IsNullOrEmpty(middleNameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите ФИО полностью.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $FirstName AS Utf8;
                    DECLARE $LastName AS Utf8;
                    DECLARE $MiddleName AS Utf8;
                    DECLARE $Phone AS Utf8;

                    UPSERT INTO Customers (Id, FirstName, LastName, MiddleName, Phone) VALUES
                        ($Id, $FirstName, $LastName, $MiddleName, $Phone);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) },
                { "$FirstName", YdbValue.MakeUtf8(firstNameTextBox.Text.Trim()) },
                { "$LastName", YdbValue.MakeUtf8(lastNameTextBox.Text.Trim()) },
                { "$MiddleName", YdbValue.MakeUtf8(middleNameTextBox.Text.Trim()) },
                     { "$Phone", YdbValue.MakeUtf8(PhoneTxt.Text.Trim()) },
                        }
                );
            });

            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные успешно сохранены.", "Успех",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (MessageBox.Show("Вы уверены что хотите удалить данный элемент?", "Удаление данных",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {
                var tableClient = new TableClient(driver, new TableClientConfig());


                var response = await tableClient.SessionExec(async session =>
                {
                    var query = @$"
                        DECLARE $Id AS Uint64;        

                        DELETE FROM {connectNameTable} WHERE Id == $Id;
                    ";

                    return await session.ExecuteDataQuery(
                        query: query,
                        txControl: TxControl.BeginSerializableRW().Commit(),
                        parameters: new Dictionary<string, YdbValue>
                            {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) }
                            }
                    );
                });
                response.Status.EnsureSuccess();
                if (response.Status.StatusCode == StatusCode.Success)
                    MessageBox.Show("Элемент был удален.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                loadData();
                resetMe();
            }

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex != -1)
            {
                DataGridView dgv1 = dataGridView1;

                this.id = Convert.ToString(dgv1.CurrentRow.Cells[0].Value);
                customerUpdateButton.Text = "Изменить (" + this.id + ")";
                customerDeleteButton.Text = "Удалить (" + this.id + ")";
                productUpdateButton.Text = "Изменить (" + this.id + ")";
                productDeleteButton.Text = "Удалить (" + this.id + ")";
            }

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            nameTable = tabControl1.TabPages[tabControl1.SelectedIndex].Text;
            loadData();
            resetMe();
        }

        private async void productInsertButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите название продукта", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $Name AS Utf8;
                    DECLARE $Price AS Uint64;

                    UPSERT INTO Products (Id, Name,Price) VALUES
                        ($Id, $Name,$Price);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(GetLastIdFromTable()) },
                { "$Name", YdbValue.MakeUtf8(nameTextBox.Text.Trim()) },
                 {"$Price",YdbValue.MakeUint64(Convert.ToUInt64(priceTextBox.Text.Trim())) },

                        }
                );
            });
            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void productUpdateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.id))
            {
                MessageBox.Show("Пожалуйста выберите элемент из списка", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (string.IsNullOrEmpty(nameTextBox.Text.Trim()))
            {
                MessageBox.Show("Пожалуйста введите название.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var tableClient = new TableClient(driver, new TableClientConfig());

            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                    DECLARE $Id AS Uint64;
                    DECLARE $Name AS Utf8;
                    DECLARE $Price AS Uint64;

                    UPSERT INTO Products (Id, Name,Price) VALUES
                        ($Id, $Name,$Price);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$Id", YdbValue.MakeUint64(ulong.Parse(this.id)) },
                { "$Name", YdbValue.MakeUtf8(nameTextBox.Text.Trim()) },
                 {"$Price",YdbValue.MakeUint64(Convert.ToUInt64(priceTextBox.Text.Trim())) },
                        }
                );
            });
            response.Status.EnsureSuccess();
            if (response.Status.StatusCode == StatusCode.Success)
                MessageBox.Show("Данные сохранены.", "Успех",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            loadData();
            resetMe();
        }

        private async void ReportWorker_Click(object sender, EventArgs e)
        {
            dt.Clear();
            dt = new DataTable();
            var tableClient = new TableClient(driver, new TableClientConfig());
     
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM CustomersReport";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];



            dt.Columns.Add("Id", typeof(ulong));
            dt.Columns.Add("Фамилия", typeof(string));
            dt.Columns.Add("Имя", typeof(string));
            dt.Columns.Add("Отчество", typeof(string));
            dt.Columns.Add("Оплаченная сумма", typeof(ulong));
            dt.Columns.Add("Сумма доставок", typeof(ulong));
            dt.Columns.Add("Сумма отмен", typeof(ulong));
            foreach (var row in resultSet.Rows)
            {
                dt.Rows.Add((ulong?)row["Id"], (string?)row["LastName"],
                    (string?)row["FirstName"], (string?)row["MiddleName"],
                    (ulong?)row["Payed"], (ulong?)row["Delivered"], (ulong?)row["Cancelled"]);
            }

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }

        private async void ReportProduct_Click(object sender, EventArgs e)
        {
            dt.Clear();
            dt = new DataTable();
            var tableClient = new TableClient(driver, new TableClientConfig());
       
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @$"SELECT * FROM ProductsReport";

                return await session.ExecuteDataQuery(
                query: query,
                txControl: TxControl.BeginSerializableRW().Commit()
                );
            });
            response.Status.EnsureSuccess();
            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSet = queryResponse.Result.ResultSets[0];



            dt.Columns.Add("Id", typeof(ulong));
            dt.Columns.Add("Название товара", typeof(string));
            dt.Columns.Add("Товаров может быть доставлено", typeof(ulong));
            dt.Columns.Add("На сумму", typeof(ulong));
            dt.Columns.Add("Общее число заказанных товаров", typeof(ulong));
            foreach (var row in resultSet.Rows)
            {
                dt.Rows.Add((ulong?)row["Id"], (string?)row["Name"], (ulong?)row["MayBeDeliveredProduct"],
                    (ulong?)row["PriceProducts"], (ulong?)row["TotalOrderedProduct"]);
            }

            DataGridView dgv1 = dataGridView1;

            dgv1.MultiSelect = false;
            dgv1.AutoGenerateColumns = true;
            dgv1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgv1.DataSource = dt;

            dgv1.Columns[0].Width = 55;
        }
    }
}