﻿using MIMS.Entity.Model;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MIMS.Entity.Dtos;

namespace MIMS.Service
{
    public class PSS_InWarehouseDetailDAL
    {

        #region init dbconnection
        private static readonly string connString = ConfigurationManager.ConnectionStrings["PharmacySystem"].ConnectionString;
        private IDbConnection _conn;
        public IDbConnection Conn
        {
            get
            {
                _conn = new SqlConnection(connString);
                _conn.Open();
                return _conn;
            }
        }
        #endregion

        /// <summary>
        /// 获取一个list的数据
        /// </summary>
        /// <returns></returns>
        public IList GetList(Dictionary<string, object> prams, string where = null)
        {
            using (Conn)
            {
                string query = @"SELECT * FROM(SELECT IW.*,M.Name,I.IWDate,C.CompanyName FROM PSS_InWarehouseDetail IW
                                        LEFT JOIN PHA_Accounts A ON IW.PhaCode = A.PhaCode AND IW.OrginID = A.OrginID
                                        LEFT JOIN PSS_InWarehouseMode M ON IW.IWWay = M.ID
                                        LEFT JOIN PSS_InWarehouse I ON IW.IWID = I.IWID
                                        LEFT JOIN PSS_PurchaseCompany C ON A.CompanyID = C.CompanyID)A WHERE 1=1  ";
                if (!string.IsNullOrEmpty(where))
                    query += where;
                return Conn.Query<Dto_InWarehouseDetail>(query, prams).ToList();
            }

        }
        /// <summary>
        /// 分页获取数据列表(带条件)
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="orderField">排序字段</param>
        /// <param name="prams">参数</param>
        /// <param name="orderType">排序类型</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="count">总条数</param>
        /// <returns></returns>
        public IList GetPageListWhere(StringBuilder where, Dictionary<string, object> prams, string orderField, string orderType, int pageIndex, int pageSize, ref int count)
        {
            int num = (pageIndex - 1) * pageSize;
            int num1 = pageIndex * pageSize;
            using (Conn)
            {
                StringBuilder strSql = new StringBuilder();
                StringBuilder sql = new StringBuilder();
                sql.Append(@"SELECT * FROM (SELECT  I.*,
													B.PinyinCode,
													A.Stock,
													B.PhaName,
													B.Spec,
													B.Unit,
													O.OrginName,
													C.CompanyName
														FROM PSS_InWarehouseDetail I
													LEFT JOIN PHA_Accounts A ON I.PhaCode = A.PhaCode AND I.OrginID = A.OrginID
													LEFT JOIN PHA_BaseInfo B ON I.PhaCode = B.PhaCode
													LEFT JOIN PHA_Orgin O ON I.OrginID = O.OrginID
													LEFT JOIN PSS_PurchaseCompany C ON A.CompanyID = C.CompanyID) A WHERE 1=1 ");
                sql.Append(where);
                strSql.Append("Select * From (Select ROW_NUMBER() Over (Order By " + orderField + " " + orderType + "");
                strSql.Append(") As rowNum, * From (" + sql + ") As T ) As N Where rowNum > " + num + " And rowNum <= " + num1 + "");
                count = Conn.Query<int>("Select Count(1) From (" + sql + ") As t", prams).Single();
                return Conn.Query<Dto_InWarehouseDetail>(strSql.ToString(), prams).ToList();
            }
        }

        /// <summary>
        /// 根据主键值获得一个对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PSS_InWarehouseDetail GetEntity(string id)
        {
            using (Conn)
            {
                string query = "SELECT * FROM PSS_InWarehouseDetail WHERE ID=@ID";
                return Conn.Query<PSS_InWarehouseDetail>(query, new { ID = id }).SingleOrDefault();
            }
        }
        public int Insert(PSS_InWarehouseDetail obj)
        {
            obj.InWarehouseSum = obj.InWarehousePrice * obj.InWarehouseCount;
            using (Conn)
            {
                string query = @"INSERT INTO PSS_InWarehouseDetail 
									VALUES(@IWID,@IWWay,@PhaCode,@OrginID,@PhaNo,@PhaExpiry,@InWarehouseCount,@InWarehousePrice,@InWarehouseSum)";
                return Conn.Execute(query, obj);
            }
        }
        public int Delete(PSS_InWarehouseDetail obj)
        {
            using (Conn)
            {
                string query = @"DELETE FROM PSS_InWarehouseDetail WHERE ID = @ID";
                return Conn.Execute(query, obj);
            }
        }


        /// <summary>
        /// 分页获取符合条件的入货药品记录
        /// </summary>
        /// <param name="where"></param>
        /// <param name="prams"></param>
        /// <param name="orderField"></param>
        /// <param name="orderType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList SearchInDatePha(StringBuilder where, Dictionary<string, object> prams, string orderField, string orderType, int pageIndex, int pageSize, ref int count)
        {
            int num = (pageIndex - 1) * pageSize;
            int num1 = pageIndex * pageSize;
            using (Conn)
            {
                StringBuilder strSql = new StringBuilder();
                StringBuilder sql = new StringBuilder();
                sql.Append(@"SELECT * FROM (SELECT IW.PhaCode,
                                                    IW.OrginID,
                                                    IW.InWarehouseCount,
                                                    I.IWDate,
                                                    B.PhaName,
                                                    B.Spec,
                                                    B.Unit,
                                                    B.PinyinCode,
                                                    O.OrginName
                                                     FROM PSS_InWarehouseDetail IW
                                                    LEFT JOIN PHA_BaseInfo B ON IW.PhaCode = B.PhaCode
                                                    LEFT JOIN PHA_Orgin O ON IW.OrginID = O.OrginID
                                                    LEFT JOIN PSS_InWarehouse I ON IW.IWID = I.IWID) A WHERE 1=1 ");
                sql.Append(where);
                strSql.Append("Select * From (Select ROW_NUMBER() Over (Order By " + orderField + " " + orderType + "");
                strSql.Append(") As rowNum, * From (" + sql + ") As T ) As N Where rowNum > " + num + " And rowNum <= " + num1 + "");
                count = Conn.Query<int>("Select Count(1) From (" + sql + ") As t", prams).Single();
                return Conn.Query<Dto_InWarehouseDetail>(strSql.ToString(), prams).ToList();
            }
        }
    }
}
