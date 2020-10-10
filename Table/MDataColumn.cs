using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.IO;



namespace CYQ.Data.Table
{
    /// <summary>
    /// ͷ�б�����
    /// </summary>
    public partial class MDataColumn
    {
        List<MCellStruct> structList;
        internal MDataTable _Table;
        internal MDataColumn(MDataTable table)
        {
            structList = new List<MCellStruct>();
            _Table = table;
        }

        public MDataColumn()
        {
            structList = new List<MCellStruct>();
        }
        /// <summary>
        /// �����Ƿ���
        /// </summary>
        internal bool IsColumnNameChanged = false;
        /// <summary>
        /// �洢����������
        /// </summary>
        private MDictionary<string, int> columnIndex = new MDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// ������ʱ����������Ƿ��ظ�(Ĭ��Ϊtrue)��
        /// </summary>
        public bool CheckDuplicate = true;
        /// <summary>
        /// ��ʽת����ͷ
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static implicit operator MDataColumn(DataColumnCollection columns)
        {
            if (columns == null)
            {
                return null;
            }
            MDataColumn mColumns = new MDataColumn();

            if (columns.Count > 0)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    MCellStruct cellStruct = new MCellStruct(columns[i].ColumnName, DataType.GetSqlType(columns[i].DataType), columns[i].ReadOnly, columns[i].AllowDBNull, columns[i].MaxLength);
                    mColumns.Add(cellStruct);
                }
            }
            return mColumns;
        }

        public MCellStruct this[string key]
        {
            get
            {
                int index = GetIndex(key);
                if (index > -1)
                {
                    return this[index];
                }
                return null;
            }
        }
        /// <summary>
        /// �ܹ������õı�
        /// </summary>
        public MDataTable Table
        {
            get
            {
                return _Table;
            }
        }
        private string _Description = string.Empty;
        /// <summary>
        /// ��������
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        private string _TableName = string.Empty;
        /// <summary>
        /// ����
        /// </summary>
        public string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
            }
        }



        public MDataColumn Clone()
        {
            MDataColumn mcs = new MDataColumn();
            mcs.DataBaseType = DataBaseType;
            mcs.CheckDuplicate = false;
            mcs.isViewOwner = isViewOwner;
            mcs.TableName = TableName;
            mcs.Description = Description;
            foreach (string item in relationTables)
            {
                mcs.AddRelateionTableName(item);
            }
            for (int i = 0; i < this.Count; i++)
            {
                mcs.Add(this[i].Clone());
            }
            return mcs;
        }
        public bool Contains(string columnName)
        {
            return GetIndex(columnName) > -1;
        }

        /// <summary>
        /// ��ȡ�����ڵ�����λ��(�������ڷ���-1��
        /// </summary>
        public int GetIndex(string columnName)
        {
            columnName = columnName.Trim();
            if (columnIndex.Count == 0 || IsColumnNameChanged || columnIndex.Count != Count)
            {
                columnIndex.Clear();
                for (int i = 0; i < Count; i++)
                {
                    columnIndex.Add(this[i].ColumnName.Replace("_", ""), i);
                }
                IsColumnNameChanged = false;
            }

            if (!string.IsNullOrEmpty(columnName))
            {
                if (columnName.IndexOf('_') > -1)
                {
                    columnName = columnName.Replace("_", "");//����ӳ�䴦��
                }
                if (columnIndex.ContainsKey(columnName))
                {
                    return columnIndex[columnName];
                }
                else
                {
                    //����Ĭ��ǰ׺���
                    string[] items = AppConfig.UI.AutoPrefixs.Split(',');
                    if (items != null && items.Length > 0)
                    {
                        foreach (string item in items)
                        {
                            columnName = columnName.StartsWith(item) ? columnName.Substring(3) : item + columnName;
                            if (columnIndex.ContainsKey(columnName))
                            {
                                return columnIndex[columnName];
                            }
                        }
                    }
                }
                //for (int i = 0; i < Count; i++)
                //{
                //    if (string.Compare(this[i].ColumnName.Replace("_", ""), columnName, StringComparison.OrdinalIgnoreCase) == 0)//������������StringComparison.OrdinalIgnoreCase����true�졣
                //    {
                //        return i;
                //    }
                //}
            }
            return -1;
        }
        /// <summary>
        /// �� �� ����Ż�λ�ø���Ϊָ������Ż�λ�á�
        /// </summary>
        /// <param name="columnName">����</param>
        /// <param name="ordinal">���</param>
        public void SetOrdinal(string columnName, int ordinal)
        {
            int index = GetIndex(columnName);
            if (index > -1 && index != ordinal)
            {
                MCellStruct mstruct = this[index];
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    List<object> items = _Table.GetColumnItems<object>(index, BreakOp.None);
                    _Table.Columns.RemoveAt(index);
                    _Table.Columns.Insert(ordinal, mstruct);
                    for (int i = 0; i < items.Count; i++)
                    {
                        _Table.Rows[i].Set(ordinal, items[i]);
                    }
                    items = null;
                }
                else
                {
                    structList.RemoveAt(index);//�Ƴ�
                    if (ordinal >= Count)
                    {
                        ordinal = Count;
                    }
                    structList.Insert(ordinal, mstruct);
                }
            }
            columnIndex.Clear();
        }

        /// <summary>
        /// ���Json��ʽ�ı�����
        /// </summary>
        public string ToJson(bool isFullSchema)
        {
            JsonHelper helper = new JsonHelper();
            helper.Fill(this, isFullSchema);
            return helper.ToString();
        }
        /// <summary>
        /// ת����
        /// </summary>
        /// <param name="tableName">����</param>
        /// <returns></returns>
        public MDataRow ToRow(string tableName)
        {
            MDataRow row = new MDataRow(this);
            row.TableName = tableName;
            row.Columns.CheckDuplicate = CheckDuplicate;
            row.Columns.DataBaseType = DataBaseType;
            row.Columns.isViewOwner = isViewOwner;
            row.Columns.relationTables = relationTables;
            row.Conn = Conn;
            return row;
        }
        /// <summary>
        /// ������ܹ����ⲿ�ļ���(json��ʽ��
        /// </summary>
        public bool WriteSchema(string fileName)
        {
            string schema = ToJson(true).Replace("},{", "},\r\n{");//д���ı�ʱҪ���С�
            return IOHelper.Write(fileName, schema);
        }

        private List<MCellStruct> _JointPrimary = new List<MCellStruct>();
        /// <summary>
        /// ��������
        /// </summary>
        public List<MCellStruct> JointPrimary
        {
            get
            {
                MCellStruct autoIncrementCell = null;
                if (_JointPrimary.Count == 0 && this.Count > 0)
                {
                    foreach (MCellStruct item in this)
                    {
                        if (item.IsPrimaryKey)
                        {
                            _JointPrimary.Add(item);
                        }
                        else if (item.IsAutoIncrement)
                        {
                            autoIncrementCell = item;
                        }
                    }
                    if (_JointPrimary.Count == 0)
                    {
                        if (autoIncrementCell != null)
                        {
                            _JointPrimary.Add(autoIncrementCell);
                        }
                        else
                        {
                            _JointPrimary.Add(this[0]);
                        }
                    }
                }
                return _JointPrimary;
            }
        }

        /// <summary>
        /// ��һ������
        /// </summary>
        public MCellStruct FirstPrimary
        {
            get
            {
                if (JointPrimary.Count > 0)
                {
                    return JointPrimary[0];
                }
                return null;
            }
        }
        /// <summary>
        /// �׸�Ψһ��
        /// </summary>
        public MCellStruct FirstUnique
        {
            get
            {
                MCellStruct ms = null;
                foreach (MCellStruct item in this)
                {
                    if (item.IsUniqueKey)
                    {
                        return item;
                    }
                    else if (ms == null && !item.IsPrimaryKey && DataType.GetGroup(item.SqlType) == 0)//ȡ��һ���ַ�������
                    {
                        ms = item;
                    }
                }
                if (ms == null && this.Count > 0)
                {
                    ms = this[0];
                }
                return ms;
            }
        }

        /// <summary>
        /// ��ǰ�����ݿ����͡�
        /// </summary>
        internal DataBaseType DataBaseType = DataBaseType.None;
        /// <summary>
        /// ��ǰ�����ݿ����������䣩
        /// </summary>
        internal string Conn = string.Empty;
        /// <summary>
        /// �ýṹ�Ƿ�����ͼӵ��
        /// </summary>
        internal bool isViewOwner = false;
        internal List<string> relationTables = new List<string>();
        internal void AddRelateionTableName(string tableName)
        {
            if (!string.IsNullOrEmpty(tableName) && !relationTables.Contains(tableName))
            {
                relationTables.Add(tableName);
            }
        }

        /// <summary>
        /// �����ṹ������ת��Table��ʾ
        /// </summary>
        /// <returns></returns>
        public MDataTable ToTable()
        {
            string tableName = string.Empty;
            if (_Table != null)
            {
                tableName = _Table.TableName;
            }
            MDataTable dt = new MDataTable(tableName);
            dt.Columns.Add("ColumnName");
            dt.Columns.Add("MaxSize");
            dt.Columns.Add("Scale");
            dt.Columns.Add("IsCanNull");
            dt.Columns.Add("IsAutoIncrement");
            dt.Columns.Add("SqlType");
            dt.Columns.Add("IsPrimaryKey");
            dt.Columns.Add("IsUniqueKey");
            dt.Columns.Add("IsForeignKey");
            dt.Columns.Add("FKTableName");
            dt.Columns.Add("DefaultValue");
            dt.Columns.Add("Description");
            dt.Columns.Add("TableName");

            for (int i = 0; i < Count; i++)
            {
                MCellStruct ms = this[i];
                dt.NewRow(true)
                    .Set(0, ms.ColumnName)
                    .Set(1, ms.MaxSize)
                    .Set(2, ms.Scale)
                    .Set(3, ms.IsCanNull)
                    .Set(4, ms.IsAutoIncrement)
                    .Set(5, ms.SqlType)
                    .Set(6, ms.IsPrimaryKey)
                    .Set(7, ms.IsUniqueKey)
                    .Set(8, ms.IsForeignKey)
                    .Set(9, ms.FKTableName)
                    .Set(10, ms.DefaultValue)
                    .Set(11, ms.Description)
                .Set(12, ms.TableName);
            }
            return dt;
        }


    }
    public partial class MDataColumn : IList<MCellStruct>
    {
        public int Count
        {
            get { return structList.Count; }
        }

        #region Add���ط���
        /// <summary>
        /// ������
        /// </summary>
        /// <param name="columnName">����</param>
        public void Add(string columnName)
        {
            Add(columnName, SqlDbType.NVarChar, false, true, -1, false, null);
        }
        /// <param name="SqlType">�е���������</param>
        public void Add(string columnName, SqlDbType sqlType)
        {
            Add(columnName, sqlType, false, true, -1, false, null);
        }
        /// <param name="isAutoIncrement">�Ƿ�����id��</param>
        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement)
        {
            Add(columnName, sqlType, isAutoIncrement, !isAutoIncrement, -1, isAutoIncrement, null);
        }

        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, int maxSize)
        {
            Add(columnName, sqlType, isAutoIncrement, isCanNull, maxSize, false, null);
        }

        /// <param name="defaultValue">Ĭ��ֵ[���������봫��SqlValue.GetDate]</param>
        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, int maxSize, bool isPrimaryKey, object defaultValue)
        {
            string[] items = columnName.Split(',');
            foreach (string item in items)
            {
                MCellStruct mdcStruct = new MCellStruct(item, sqlType, isAutoIncrement, isCanNull, maxSize);
                mdcStruct.IsPrimaryKey = isPrimaryKey;
                mdcStruct.DefaultValue = defaultValue;
                Add(mdcStruct);
            }
        }

        #endregion

        public void Add(MCellStruct item)
        {
            if (item != null && !this.Contains(item) && (!CheckDuplicate || !Contains(item.ColumnName)))
            {
                if (DataBaseType == DataBaseType.None)
                {
                    DataBaseType = item.DalType;
                }
                item.MDataColumn = this;
                structList.Add(item);
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    for (int i = 0; i < _Table.Rows.Count; i++)
                    {
                        if (Count > _Table.Rows[i].Count)
                        {
                            _Table.Rows[i].Add(new MDataCell(ref item));
                        }
                    }
                }
                columnIndex.Clear();
            }
        }


        //public void AddRange(IEnumerable<MCellStruct> collection)
        //{
        //    AddRange(collection as MDataColumn);
        //}
        public void AddRange(MDataColumn items)
        {
            if (items.Count > 0)
            {
                foreach (MCellStruct item in items)
                {
                    if (!Contains(item.ColumnName))
                    {
                        Add(item);
                    }
                }
                columnIndex.Clear();
            }
        }


        public bool Remove(MCellStruct item)
        {
            Remove(item.ColumnName);
            return true;
        }
        public void Remove(string columnName)
        {
            string[] items = columnName.Split(',');
            foreach (string item in items)
            {
                int index = GetIndex(item);
                if (index > -1)
                {
                    RemoveAt(index);
                    columnIndex.Clear();
                }
            }
        }

        public void RemoveRange(int index, int count) // 1,4
        {
            for (int i = index; i < index + count; i++)
            {
                RemoveAt(index);//ÿ��ɾ�����ƶ���������������ɾ��N�μ��ɡ�
            }
        }
        public void RemoveAt(int index)
        {
            structList.RemoveAt(index);
            if (_Table != null)
            {
                foreach (MDataRow row in _Table.Rows)
                {
                    if (row.Count > Count)
                    {
                        row.RemoveAt(index);
                    }
                }
            }
            columnIndex.Clear();

        }


        public void Insert(int index, MCellStruct item)
        {
            if (item != null && !this.Contains(item) && (!CheckDuplicate || !Contains(item.ColumnName)))
            {
                item.MDataColumn = this;
                structList.Insert(index, item);
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    for (int i = 0; i < _Table.Rows.Count; i++)
                    {
                        if (Count > _Table.Rows[i].Count)
                        {
                            _Table.Rows[i].Insert(index, new MDataCell(ref item));
                        }
                    }
                }
                columnIndex.Clear();
            }

        }
        public void InsertRange(int index, MDataColumn mdc)
        {
            for (int i = mdc.Count; i >= 0; i--)
            {
                Insert(index, mdc[i]);//����
            }
        }

        #region IList<MCellStruct> ��Ա

        int IList<MCellStruct>.IndexOf(MCellStruct item)
        {
            return structList.IndexOf(item);
        }

        #endregion

        #region ICollection<MCellStruct> ��Ա

        void ICollection<MCellStruct>.CopyTo(MCellStruct[] array, int arrayIndex)
        {
            structList.CopyTo(array, arrayIndex);
        }


        bool ICollection<MCellStruct>.IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region IEnumerable<MCellStruct> ��Ա

        IEnumerator<MCellStruct> IEnumerable<MCellStruct>.GetEnumerator()
        {
            return structList.GetEnumerator();
        }

        #endregion

        #region IEnumerable ��Ա

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return structList.GetEnumerator();
        }

        #endregion

        #region ICollection<MCellStruct> ��Ա


        public void Clear()
        {
            structList.Clear();
        }

        public bool Contains(MCellStruct item)
        {
            return structList.Contains(item);
        }

        #endregion

        #region IList<MCellStruct> ��Ա

        /// <summary>
        /// ReadOnly
        /// </summary>
        public MCellStruct this[int index]
        {
            get
            {
                return structList[index];
            }
            set
            {
                Error.Throw(AppConst.Global_NotImplemented);
            }
        }

        #endregion
    }
    public partial class MDataColumn
    {
        /// <summary>
        /// ��Json���ļ��м��س�����Ϣ
        /// </summary>
        /// <param name="jsonOrFileName">Json���ļ�������·��������</param>
        /// <returns></returns>
        public static MDataColumn CreateFrom(string jsonOrFileName)
        {
            return CreateFrom(jsonOrFileName, true);
        }
        /// <summary>
        /// ��Json���ļ��м��س�����Ϣ
        /// </summary>
        /// <param name="jsonOrFileName">Json���ļ�������·��������</param>
        /// <param name="readTxtOrXml">�Ƿ��.txt��.xml�ļ��ж�ȡ�ܹ���Ĭ��Ϊtrue��</param>
        /// <returns></returns>
        public static MDataColumn CreateFrom(string jsonOrFileName, bool readTxtOrXml)
        {
            MDataColumn mdc = new MDataColumn();
            MDataTable dt = null;
            try
            {
                bool isTxtOrXml = false;
                string json = string.Empty;
                string exName = Path.GetExtension(jsonOrFileName);
                switch (exName.ToLower())
                {
                    case ".ts":
                    case ".xml":
                    case ".txt":
                        string tsFileName = jsonOrFileName.Replace(exName, ".ts");
                        if (File.Exists(tsFileName))
                        {
                            json = IOHelper.ReadAllText(tsFileName);
                        }
                        else if (readTxtOrXml && File.Exists(jsonOrFileName))
                        {
                            isTxtOrXml = true;
                            if (exName == ".xml")
                            {
                                json = IOHelper.ReadAllText(jsonOrFileName, 0, Encoding.UTF8);
                            }
                            else if (exName == ".txt")
                            {
                                json = IOHelper.ReadAllText(jsonOrFileName);
                            }
                        }

                        break;
                    default:
                        json = jsonOrFileName;
                        break;
                }
                if (!string.IsNullOrEmpty(json))
                {
                    dt = MDataTable.CreateFrom(json);
                    if (json != jsonOrFileName)
                    {
                        dt.TableName = Path.GetFileNameWithoutExtension(jsonOrFileName);
                    }
                    if (dt.Columns.Count > 0)
                    {
                        if (isTxtOrXml)
                        {
                            mdc = dt.Columns.Clone();
                        }
                        else
                        {
                            foreach (MDataRow row in dt.Rows)
                            {
                                MCellStruct cs = new MCellStruct(
                                    row.Get<string>("ColumnName"),
                                    DataType.GetSqlType(row.Get<string>("SqlType", "string")),
                                    row.Get<bool>("IsAutoIncrement", false),
                                    row.Get<bool>("IsCanNull", false),
                                    row.Get<int>("MaxSize", -1));
                                cs.Scale = row.Get<short>("Scale");
                                cs.IsPrimaryKey = row.Get<bool>("IsPrimaryKey", false);
                                cs.DefaultValue = row.Get<string>("DefaultValue");


                                //��������
                                cs.Description = row.Get<string>("Description");
                                cs.TableName = row.Get<string>("TableName");
                                cs.IsUniqueKey = row.Get<bool>("IsUniqueKey", false);
                                cs.IsForeignKey = row.Get<bool>("IsForeignKey", false);
                                cs.FKTableName = row.Get<string>("FKTableName");
                                mdc.Add(cs);
                            }
                            mdc.TableName = dt.TableName;
                            mdc.Description = dt.Description;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
            finally
            {
                dt = null;
            }
            return mdc;
        }

        //internal bool AcceptChanges(AcceptOp op)
        //{
        //    if (_Table == null)
        //    {
        //        return false;
        //    }
        //    return AcceptChanges(op, _Table.TableName, _Table.Conn);
        //}
        //internal bool AcceptChanges(AcceptOp op, string tableName, string newConn)
        //{
        //    if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(newConn) || Count == 0)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
    }
}