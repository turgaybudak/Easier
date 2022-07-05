using FastMember;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;

namespace Easier
{
    public abstract class Easier<CType>
    {
        #region Tanımlamalar

        private static string Baglanti = "Con String";

        private string _TableName;

        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }

        private string _PrimaryKey;

        public string PrimaryKey
        {
            get { return _PrimaryKey; }
            set { _PrimaryKey = value; }
        }

        private bool _Caching;

        public bool Caching
        {
            get { return _Caching; }
            set { _Caching = value; }
        }

        #endregion Tanımlamalar

        #region Ekle Güncelle Sil

        public int Add(CType GelenTip, bool SonIDGetirilsinmi = true, string[] KontrolDegerleri = null)
        {
            Type Tip = GelenTip.GetType();
            PropertyInfo[] Ozellikler = Tip.GetProperties().Where(a => a.Name != PrimaryKey && !a.Name.Contains("Ek_")).ToArray();
            SqlCommand cmd = new SqlCommand();

            for (int i = 0; i < Ozellikler.Length; i++)
            {
                SqlParameter Yeni = new SqlParameter();
                Yeni.ParameterName = ("@" + Ozellikler[i].Name);
                Yeni.DbType = (DBTipler[Ozellikler[i].PropertyType]);
                Yeni.Value = (Yeni.DbType == DbType.DateTime && DegerAl(GelenTip, Ozellikler[i].Name) == null ? DBNull.Value : DegerAl(GelenTip, Ozellikler[i].Name));
                cmd.Parameters.Add(Yeni);
            }

            cmd.CommandText = HazirlayiciEkle(Ozellikler.Select(a => a.Name).ToArray(), SonIDGetirilsinmi, KontrolDegerleri);

            var Sonuc = ExecuteAdapter(cmd);

            int SonIDveyaSonuc = (Sonuc.Rows.Count == 0 ? 0 : int.Parse(Sonuc.Rows[0][0].ToString()));


            if (Caching && SonIDveyaSonuc != 0)
                CachedList(true);

            return SonIDveyaSonuc;
        }

        public List<int> AddList(List<CType> GelenTipler, bool SonIDGetirilsinmi = true, string[] KontrolDegerleri = null)
        {
            List<int> SonucListesi = new List<int>();

            foreach (var GelenTip in GelenTipler)
            {
                SqlCommand cmd = new SqlCommand();

                Type Tip = GelenTip.GetType();
                PropertyInfo[] Ozellikler = Tip.GetProperties().Where(a => a.Name != PrimaryKey && !a.Name.Contains("Ek_")).ToArray();

                for (int i = 0; i < Ozellikler.Length; i++)
                {
                    SqlParameter Yeni = new SqlParameter();
                    Yeni.ParameterName = ("@" + Ozellikler[i].Name);
                    Yeni.DbType = (DBTipler[Ozellikler[i].PropertyType]);
                    Yeni.Value = (Yeni.DbType == DbType.DateTime && DegerAl(GelenTip, Ozellikler[i].Name) == null ? DBNull.Value : DegerAl(GelenTip, Ozellikler[i].Name));
                    cmd.Parameters.Add(Yeni);
                }

                cmd.CommandText = HazirlayiciEkle(Ozellikler.Select(a => a.Name).ToArray(), SonIDGetirilsinmi, KontrolDegerleri);
                Task.Delay(100);
                var Sonuc = ExecuteAdapter(cmd);

                SonucListesi.Add(Sonuc.Rows.Count == 0 ? 0 : int.Parse(Sonuc.Rows[0][0].ToString()));
            }



            if (Caching)
                CachedList(true);

            return SonucListesi;
        }



        private static Dictionary<Type, DbType> DBTip;

        private static Dictionary<Type, DbType> DBTipler
        {
            get
            {
                if (DBTip == null)
                {
                    DBTip = new Dictionary<Type, DbType>();
                    DBTip[typeof(byte)] = DbType.Byte;
                    DBTip[typeof(sbyte)] = DbType.SByte;
                    DBTip[typeof(short)] = DbType.Int16;
                    DBTip[typeof(ushort)] = DbType.UInt16;
                    DBTip[typeof(int)] = DbType.Int32;
                    DBTip[typeof(uint)] = DbType.UInt32;
                    DBTip[typeof(long)] = DbType.Int64;
                    DBTip[typeof(ulong)] = DbType.UInt64;
                    DBTip[typeof(float)] = DbType.Single;
                    DBTip[typeof(double)] = DbType.Double;
                    DBTip[typeof(decimal)] = DbType.Decimal;
                    DBTip[typeof(bool)] = DbType.Boolean;
                    DBTip[typeof(string)] = DbType.String;
                    DBTip[typeof(char)] = DbType.StringFixedLength;
                    DBTip[typeof(Guid)] = DbType.Guid;
                    DBTip[typeof(DateTime)] = DbType.DateTime;
                    DBTip[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
                    DBTip[typeof(byte[])] = DbType.Binary;
                    DBTip[typeof(byte?)] = DbType.Byte;
                    DBTip[typeof(sbyte?)] = DbType.SByte;
                    DBTip[typeof(short?)] = DbType.Int16;
                    DBTip[typeof(ushort?)] = DbType.UInt16;
                    DBTip[typeof(int?)] = DbType.Int32;
                    DBTip[typeof(uint?)] = DbType.UInt32;
                    DBTip[typeof(long?)] = DbType.Int64;
                    DBTip[typeof(ulong?)] = DbType.UInt64;
                    DBTip[typeof(float?)] = DbType.Single;
                    DBTip[typeof(double?)] = DbType.Double;
                    DBTip[typeof(decimal?)] = DbType.Decimal;
                    DBTip[typeof(bool?)] = DbType.Boolean;
                    DBTip[typeof(char?)] = DbType.StringFixedLength;
                    DBTip[typeof(Guid?)] = DbType.Guid;
                    DBTip[typeof(DateTime?)] = DbType.DateTime;
                    DBTip[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;

                    return DBTip;
                }
                else
                    return DBTip;
            }
        }

        private static object DegerAl(object Nesne, string Ozellik)
        {
            return Nesne.GetType().GetProperty(Ozellik).GetValue(Nesne, null);
        }

        private string HazirlayiciEkle(string[] Degerler, bool SonIDGetirilsinmi = false, string[] KontrolDegerleri = null)
        {
            string SonID = string.Empty;
            if (SonIDGetirilsinmi)
                SonID = " SELECT SCOPE_IDENTITY() as ID;";

            string Son = ";";
            string KontrolEdilecekDegerleri = string.Empty;
            if (KontrolDegerleri != null)//Eğer Tüm Değerleri Gönderirsek Mükerrer Kaydı Engeller
            {
                KontrolEdilecekDegerleri += ("IF (SELECT Count(*) FROM " + _TableName + " WHERE ");
                KontrolEdilecekDegerleri += string.Join(" AND ", Degerler.Where(a => a != PrimaryKey && !a.Contains("Ek_")).Select(a => ("@" + a)).ToArray());
                KontrolEdilecekDegerleri += (") = 0 BEGIN ");

                Son = " END;";
            }

            return KontrolEdilecekDegerleri + @"INSERT INTO " + TableName + " (" + string.Join(",", Degerler.Where(a => a != PrimaryKey)) + ")" + "VALUES (" + string.Join(",", Degerler.Where(a => a != PrimaryKey).Select(a => ("@" + a)).ToArray()) + ")" + Son + SonID;
        }

        public int Delete(int ID)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UPDATE " + _TableName + " SET Aktifmi=0 WHERE " + _PrimaryKey + " = @" + _PrimaryKey + ";";
            cmd.Parameters.Add(new SqlParameter("@" + _PrimaryKey, ID));

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int PermanentlyDelete(int ID)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DELETE  From " + _TableName + " WHERE " + _PrimaryKey + " = @" + _PrimaryKey + ";";
            cmd.Parameters.Add(new SqlParameter("@" + _PrimaryKey, ID));

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int Delete(List<int> IDler)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UPDATE " + _TableName + " SET Aktifmi=0 WHERE " + _PrimaryKey + " in (" + string.Join(",", IDler) + ");";

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int UnDeleted(int ID)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UPDATE " + _TableName + " SET Aktifmi=1 WHERE " + _PrimaryKey + " = @" + _PrimaryKey + ";";
            cmd.Parameters.Add(new SqlParameter("@" + _PrimaryKey, ID));

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int Delete(List<SqlParameter> Filtre)
        {
            List<SqlParameter> SilDegeri = new List<SqlParameter> { new SqlParameter { ParameterName = "@Aktifmi", Value = false } };
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = HazirlayiciGuncelle(SilDegeri.Select(x => x.ParameterName.Replace("@", "")).ToArray(), Filtre.Select(x => x.ParameterName.Replace("@", "")).ToArray());
            cmd.Parameters.AddRange(SilDegeri.Concat(Filtre).ToArray<SqlParameter>());

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int Update(int ID, CType Gelen) //henüz Tavsiye edilmez
        {
            List<SqlParameter> Duzenlenenler = new List<SqlParameter>();

            foreach (var property in Gelen.GetType().GetProperties().Where(a => a.Name != PrimaryKey && a.GetValue(Gelen) != null))
                Duzenlenenler.Add(new SqlParameter { ParameterName = property.Name, Value = property.GetValue(Gelen) });

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = HazirlayiciGuncelle(Duzenlenenler.Select(x => x.ParameterName.Replace("@", "")).ToArray(), new string[] { _PrimaryKey });
            cmd.Parameters.AddRange(Duzenlenenler.ToArray<SqlParameter>());
            cmd.Parameters.Add(new SqlParameter { ParameterName = ("@" + _PrimaryKey), Value = ID });

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int Update(int ID, List<SqlParameter> Duzenlenenler) //henüz Tavsiye edilmez
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = HazirlayiciGuncelle(Duzenlenenler.Select(x => x.ParameterName.Replace("@", "")).ToArray(), new string[] { _PrimaryKey });
            cmd.Parameters.AddRange(Duzenlenenler.ToArray<SqlParameter>());
            cmd.Parameters.Add(new SqlParameter { ParameterName = ("@" + _PrimaryKey), Value = ID });

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        public int Update(List<SqlParameter> Filtre, List<SqlParameter> Duzenlenenler)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = HazirlayiciGuncelle(Duzenlenenler.Select(x => x.ParameterName.Replace("@", "")).ToArray(), Filtre.Select(x => x.ParameterName.Replace("@", "")).ToArray());
            cmd.Parameters.AddRange(Duzenlenenler.Concat(Filtre).ToArray<SqlParameter>());

            int Sonuc = ExecuteNonQuery(cmd);

            if (Caching && Sonuc != 0)
                CachedList(true);

            return Sonuc;
        }

        internal string HazirlayiciGuncelle(string[] Duzenlenenler, string[] FiltreDegerleri)
        {
            return @"UPDATE " + TableName + " SET " + string.Join(",", Duzenlenenler.Select(a => (a + "=@" + a))) + " WHERE " + string.Join(" AND ", FiltreDegerleri.Select(a => (a + "=@" + a)));
        }

        #endregion Ekle Güncelle Sil

        #region Listelemeler

        public List<CType> Search(List<SqlParameter> Filtre, bool SilinenleriGetir = false)
        {
            SqlCommand cmd = new SqlCommand();

            if (!SilinenleriGetir)
                Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });

            cmd.CommandText = "SELECT * FROM " + TableName + " WHERE " + string.Join(" AND ", Filtre.Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", ""))));
            cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
            DataTable dt = ExecuteAdapter(cmd);
            return ListeyeCevir<CType>(dt);
        }

        public List<CType> Search(string Operator, List<SqlParameter> Filtre, bool SilinenleriGetir = false)
        {
            SqlCommand cmd = new SqlCommand();

            if (!SilinenleriGetir)
                Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });

            cmd.CommandText = "SELECT * FROM " + TableName + " WHERE " + (Filtre.Any(a => a.ParameterName == "Aktifmi") ? " Aktifmi=1 " + (Filtre.Any(a => a.ParameterName != "Aktifmi") ? " AND " : "") : "") + (Filtre.Any(a => a.ParameterName != "Aktifmi") ? "(" : "") + string.Join(Operator, Filtre.Where(a => a.ParameterName != "Aktifmi").Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", "")))) + (Filtre.Any(a => a.ParameterName != "Aktifmi") ? ")" : "");
            cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
            DataTable dt = ExecuteAdapter(cmd);
            return ListeyeCevir<CType>(dt);
        }

        public List<CType> Search(List<int> IDler, bool Dahil = true)
        {
            SqlCommand cmd = new SqlCommand();

            string IDduzen = @"
                                        DECLARE @Istenenler TABLE(id int );
	                                    DECLARE @Number VARCHAR(max);
	                                    DECLARE @Ayrac CHAR;
	                                    SET @Ayrac = ','
		                                    WHILE CHARINDEX(@Ayrac, @IDs) > 0
	                                    BEGIN
		                                    SET @Number = SUBSTRING(@IDs, 0, CHARINDEX(@Ayrac, @IDs))
		                                    insert into @Istenenler values (convert(int,@Number))
		                                    SET @IDs = SUBSTRING(@IDs, CHARINDEX(@Ayrac, @IDs) + 1, LEN(@IDs))
	                                    END
                                ";

            cmd.CommandText = IDduzen + " SELECT * FROM " + TableName + " WHERE Aktifmi=1  " + (IDler.Any() ? " AND " + ((Dahil ? "" : " NOT ") + PrimaryKey + " in (select * from  @Istenenler)") : "");

            cmd.Parameters.AddWithValue("@IDs", string.Join(",", IDler) + ",");

            DataTable dt = ExecuteAdapter(cmd);

            return ListeyeCevir<CType>(dt);
        }

        public int SearchCount(List<SqlParameter> Filtre, bool SilinenleriSay = false)
        {
            SqlCommand cmd = new SqlCommand();
            if (!SilinenleriSay)
                Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });

            cmd.CommandText = "SELECT Count(*) FROM " + TableName + " WHERE " + string.Join(" AND ", Filtre.Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", ""))));
            cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
            return ExecuteScalarInt(cmd);
        }

        public CType Get(int ID, bool SilinenleriGetir = false)
        {
            SqlCommand cmd = new SqlCommand();
            CType Gonderilecek;
            List<SqlParameter> Filtre = new List<SqlParameter>();
            Filtre.Add(new SqlParameter { ParameterName = PrimaryKey, Value = ID });
            if (!SilinenleriGetir)
                Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });

            cmd.CommandText = "SELECT * FROM " + TableName + " WHERE " + string.Join(" AND ", Filtre.Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", ""))));
            cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
            DataTable dt = ExecuteAdapter(cmd);
            List<CType> Gelen = ListeyeCevir<CType>(dt);

            if (Gelen.Any())
            {
                return Gelen.First();
            }
            else
                return default(CType);
        }

        protected List<CType> ListCaching()
        {
            SqlCommand cmd = new SqlCommand();
            List<SqlParameter> Filtre = new List<SqlParameter>();
            Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });
            cmd.CommandText = "SELECT  * FROM " + TableName + " WHERE " + string.Join(" AND ", Filtre.Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", ""))));
            cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
            DataTable dt = ExecuteAdapter(cmd);
            var Gelen = ListeyeCevir<CType>(dt);
            return Gelen;
        }

        public List<CType> List(int ElemanSayisi = 0, string SiralamaKolonlari = "", bool SilinenleriGetir = false)
        {
            if (!Caching || !EasierCaches.IsCached(TableName))
            {
                SqlCommand cmd = new SqlCommand();
                List<SqlParameter> Filtre = new List<SqlParameter>();
                if (!SilinenleriGetir)
                    Filtre.Add(new SqlParameter { ParameterName = "Aktifmi", Value = true });

                cmd.CommandText = "SELECT " + (ElemanSayisi != 0 ? (" TOP (" + ElemanSayisi + ") ") : "") + " * FROM " + TableName + (SilinenleriGetir ? "" : " WHERE ") + string.Join(" AND ", Filtre.Select(a => (a.ParameterName + "=@" + a.ParameterName.Replace("@", "")))) + (SiralamaKolonlari != string.Empty ? (" ORDER BY " + SiralamaKolonlari) : string.Empty);
                cmd.Parameters.AddRange(Filtre.Select(a => new SqlParameter { ParameterName = "@" + a.ParameterName.Replace("@", ""), Value = a.Value }).ToArray());
                DataTable dt = ExecuteAdapter(cmd);
                var Gelen = ListeyeCevir<CType>(dt);

                if (Caching)
                    return ((List<CType>)EasierCaches.CachedSet(TableName, Gelen));
                else
                    return Gelen;
            }
            else
            {
                return CachedList();
            }
        }

        private static List<CType> ListeyeCevir<CType>(DataTable dt)
        {
            var fields = typeof(CType).GetProperties();

            List<CType> lst = new List<CType>();

            foreach (DataRow dr in dt.Rows)
            {
                var ob = Activator.CreateInstance<CType>();
                foreach (var fieldInfo in fields)
                {
                    object value = dr[fieldInfo.Name];

                    if (value == DBNull.Value)
                        fieldInfo.SetValue(ob, null);
                    else
                        fieldInfo.SetValue(ob, value);
                }
                lst.Add(ob);
            }

            return lst;
        }

        #endregion Listelemeler

        #region Caching

        private static List<CType> _CachedList;

        private List<CType> CachedList(bool Changed = false)
        {
            if (!EasierCaches.IsCached(TableName) || Changed)
                return ((List<CType>)EasierCaches.CachedSet(TableName, ListCaching()));
            else
                return ((List<CType>)EasierCaches.Cached(TableName));

            /// iyisi olabilir ama ben dbden direk yapılan değişimleri kaçırmak istemedim.
        }

        public void CachedReset()
        {
            EasierCaches.CachedSet(TableName, ListCaching());
            /// iyisi olabilir ama ben dbden direk yapılan değişimleri kaçırmak istemedim.
        }

        #endregion Caching

        #region Execute işlemleri

        protected DataTable ExecuteAdapter(SqlCommand cmd)
        {
            SqlConnection cn;
            SqlDataAdapter da = new SqlDataAdapter();
            DataTable dt = new DataTable();
            cn = new SqlConnection(Baglanti);
            cmd.Connection = cn;
            da.SelectCommand = cmd;

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 60;

                using (TransactionScope scope = new TransactionScope())
                {
                    da.Fill(dt);
                    cn.Close();

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
            }

            return dt;
        }

        internal static int ExecuteNonQuery(SqlCommand cmd)
        {
            SqlConnection cn = new SqlConnection(Baglanti);
            cmd.Connection = cn;

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 99;

                using (TransactionScope scope = new TransactionScope())
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    cn.Close();

                    //  LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                    scope.Complete();
                    return 1;
                }
            }
            catch
            {
                //  LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
                return 0;
            }
        }

        internal static Task<int> AsyncExecuteNonQuery(SqlCommand cmd)
        {
            SqlConnection cn = new SqlConnection(Baglanti);

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 30;

                using (TransactionScope scope = new TransactionScope())
                {
                    cn.Open();
                    var Sonuc = cmd.ExecuteNonQueryAsync();
                    cn.Close();

                    // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                    scope.Complete();

                    return Sonuc;
                }
            }
            catch
            {
                //LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
                return null;
            }
        }

        internal static int BackupExecuteNonQuery(SqlCommand cmd)
        {
            SqlConnection cn = new SqlConnection(Baglanti);
            cmd.Connection = cn;

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 30;

                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();

                // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "İşlem Başarılı", "***DB Yedekleme");

                return 1;
            }
            catch
            {
                // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
                return 0;
            }
        }

        internal static int ExecuteBulkQuery(DataTable GelenTablo, string AktarilacakTabloAdi)
        {
            SqlConnection cn = new SqlConnection(Baglanti);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(cn);

            try
            {
                sqlBulkCopy.DestinationTableName = AktarilacakTabloAdi;
                cn.Open();

                sqlBulkCopy.BulkCopyTimeout = 900;
                sqlBulkCopy.WriteToServer(GelenTablo);
                // LogEkle(" Toplu Kayıt Atıldı. Atılan Tablo " + sqlBulkCopy.DestinationTableName, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                return 1;
            }
            catch
            {
                // LogEkle(" Toplu Kayıt Atılırken Hata Oluştu! Atılan Tablo " + sqlBulkCopy.DestinationTableName, HttpContext.Current, "Hata", ex.ToString());
                cn.Close();

                return 0;
            }
            finally
            {
                cn.Close();
            }
        }

        internal static int ExecuteBulkQuery(List<CType> GelenListe, string AktarilacakTabloAdi)
        {
            DataTable GelenTablo = new DataTable();
            using (var reader = ObjectReader.Create(GelenListe))
            {
                GelenTablo.Load(reader);
            }

            SqlConnection cn = new SqlConnection(Baglanti);
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(cn);

            try
            {
                sqlBulkCopy.DestinationTableName = AktarilacakTabloAdi;
                cn.Open();

                sqlBulkCopy.BulkCopyTimeout = 900;
                sqlBulkCopy.WriteToServer(GelenTablo); //Dbdeki sutun abları alfabetik sırayla olmalı
                                                       // LogEkle(" Toplu Kayıt Atıldı. Atılan Tablo " + sqlBulkCopy.DestinationTableName, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                return 1;
            }
            catch
            {
                // LogEkle(" Toplu Kayıt Atılırken Hata Oluştu! Atılan Tablo " + sqlBulkCopy.DestinationTableName, HttpContext.Current, "Hata", ex.ToString());
                cn.Close();

                return 0;
            }
            finally
            {
                cn.Close();
            }
        }

        internal static object ExecuteScalarObject(SqlCommand cmd)
        {
            SqlConnection cn = new SqlConnection(Baglanti);
            cmd.Connection = cn;

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 30;

                using (TransactionScope scope = new TransactionScope())
                {
                    cn.Open();
                    object Sonuc = cmd.ExecuteScalar();
                    cn.Close();
                    // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                    scope.Complete();
                    return Sonuc;
                }
            }
            catch (TransactionAbortedException ex)
            {
                // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
                throw ex;
            }
        }

        internal static int ExecuteScalarInt(SqlCommand cmd)
        {
            SqlConnection cn = new SqlConnection(Baglanti);
            cmd.Connection = cn;

            //StringBuilder sb = new StringBuilder();
            //foreach (SqlParameter item in cmd.Parameters)
            //{
            //    sb.Append("ParameterName:" + item.ParameterName);
            //    sb.Append(";Value:" + item.Value);
            //    sb.Append(";Type:" + item.TypeName);
            //    sb.AppendLine();
            //}

            try
            {
                cmd.CommandTimeout = 30;
                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(60)))
                {
                    cn.Open();
                    int Sonuc = int.Parse(cmd.ExecuteScalar().ToString());
                    cn.Close();

                    // LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "İşlem Başarılı", "İşlem Başarılı");

                    scope.Complete();
                    return Sonuc;
                }
            }
            catch
            {
                //  LogEkle(cmd.CommandText + " " + sb, HttpContext.Current, "Hata", ex.ToString());
                return 0;
            }
        }

        #endregion Execute işlemleri
    }

    internal static class EasierCaches
    {
        private static Dictionary<string, object> _Cached;

        public static object Cached(string Key)
        {
            if (_Cached == null)
                _Cached = new Dictionary<string, object>();

            if (!_Cached.ContainsKey(Key))
                _Cached.Add(Key, null);

            return _Cached[Key];
        }

        public static bool IsCached(string Key)
        {
            if (_Cached == null)
                _Cached = new Dictionary<string, object>();

            return _Cached.ContainsKey(Key);
        }

        public static object CachedSet(string Key, object Value)
        {
            if (_Cached == null)
            {
                _Cached = new Dictionary<string, object>();
                _Cached.Add(Key, Value);
            }
            else
            {
                if (!_Cached.ContainsKey(Key))
                    _Cached.Add(Key, Value);
                else
                    _Cached[Key] = Value;
            }

            return _Cached[Key];
        }
    }
}
