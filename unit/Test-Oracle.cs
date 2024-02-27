using System;
using System.Configuration;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace unitOracle {
	[SetUpFixture]
	public class setupDb {
		public static int open() {
			Dictionary<string, object> param = new Dictionary<string, object>();
			param["connectionString"] = getConnectionString();
			param["driver"] = getDriver();
			param["cmd"] = "open";
			EdgeCompiler ec = new EdgeCompiler();
			var res = ec.CompileFunc(param);
			//var res = t(null);
			//Task.WaitAll(res);
			return (int)res.Result;
		}

		public static void close(int handler) {
			Dictionary<string, object> param = new Dictionary<string, object>();
			param["cmd"] = "close";
			param["handler"] = handler;
			param["driver"] = getDriver();
			EdgeCompiler ec = new EdgeCompiler();
			var t = ec.CompileFunc(param);
			//Task<object> resClose = t.Invoke(null);
			//Task.WaitAll(resClose);
		}

		public static string getConnectionString() {
			string connName = "oracle";
			if (System.Environment.GetEnvironmentVariable("TRAVIS") != null) {
				connName = "travis";
			}
			var configuration = new ConfigurationBuilder()
					.AddJsonFile(Path.Combine(
						AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"))
									.Build();
			return configuration.GetConnectionString(connName);


		}

		public static string getDriver() {
			var configuration = new ConfigurationBuilder()
					.AddJsonFile(Path.Combine(
						AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"))
									.Build();
			string connName = "oracle";
			if (System.Environment.GetEnvironmentVariable("TRAVIS") != null) {
				connName = "travis";
			}
			return configuration["AppSettings:driver." + connName];


		}

		public static void runScript(int handler, string script) {
			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = script,
				["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var res = ec.CompileFunc(param).Result;		
		}


		public static void runFile(int handler, string scriptName) {
			string path = Path.Combine(
					Directory.GetCurrentDirectory(), scriptName);
			string[] all = File.ReadAllLines(path);
			string script = "";
			int i = 0;
			while (i < all.Length) {
				string s = all[i];
				if (s.TrimEnd().ToUpper() == "GO") {
					if (script != "") {
						runScript(handler, script);
						script = "";
					}
				}
				else {
					script += s + "\n\r";
				}
				i = i + 1;
			}
			if (script != "") {
				runScript(handler, script);
			}
		}


		[OneTimeSetUp]
		public void runBeforeAnyTests() {
			int handler = open();
			runFile(handler, "setup-Oracle.sql");
			close(handler);
		}

		[OneTimeTearDown]
		public void runAfterAnyTests() {
			int handler = open();
			runFile(handler, "destroy-Oracle.sql");
			close(handler);
		}
	}

	[TestFixture()]
	public class Oracle {

		[Test()]
		public void compilerExists() {

			EdgeCompiler ec = new EdgeCompiler();
			Assert.IsTrue(ec.GetType().GetMethod("CompileFunc") != null, "EdgeCompiler has CompileFunc");

		}


		[Test()]
		public void openConnection() {
			Dictionary<string, object> param = new Dictionary<string, object>();
			param["connectionString"] = setupDb.getConnectionString();
			param["driver"] = setupDb.getDriver();
			param["cmd"] = "open";
			EdgeCompiler ec = new EdgeCompiler();
			var res = ec.CompileFunc(param).Result;
						
			//Assert.AreEqual(res.Status, TaskStatus.RanToCompletion, "Open executed");
			//Assert.IsFalse(res);
			Assert.IsInstanceOf(typeof(int), res, "Open returned an int");
		}

		[Test()]
		public void openBadConnection() {
			Dictionary<string, object> param = new Dictionary<string, object>();
			param["connectionString"] = "bad connection";
			param["driver"] = setupDb.getDriver();
			param["cmd"] = "open";
			param["timeout"] = 3;
			EdgeCompiler ec = new EdgeCompiler();
			
			object res = null;
			try {
				res = ec.CompileFunc(param).Result;
				//Task.WaitAll(res);
				Assert.IsFalse(true, "Open bad connection should throw");
			}
			catch {				
				Assert.AreEqual(null, res, "Open bad connection should throw");
			}
		}

		[Test()]
		public void closeConnection() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object>();
			param["cmd"] = "close";
			param["handler"] = handler;
			param["driver"] = setupDb.getDriver();
			EdgeCompiler ec = new EdgeCompiler();
			try {
				var resClose = ec.CompileFunc(param).Result;
				//Task.WaitAll(resClose);
				Assert.IsFalse(false, "Close connection should success");
			}
			catch {
				Assert.AreEqual(true, false, "Close connection should not throw");
			}			
		}

		[Test()]
		public void setupScriptShouldExist() {

			string path = Path.Combine(
						Directory.GetCurrentDirectory(), "setup-Oracle.sql");
			Assert.IsTrue(File.Exists(path), "setup script should be present in " + path);
		}

		[Test()]
		public void getDbDate() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "SELECT CURRENT_DATE FROM dual",
				["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var res = ec.CompileFunc(param).Result;
			Assert.IsInstanceOf(typeof(Dictionary<string, object>), res, "non query should return  a dictionary ");
			Assert.IsTrue(((Dictionary<string, object>)res).ContainsKey("rowcount"), "non query should return  a dictionary with rowcount");

			setupDb.close(handler);
		}



		[Test()]
		public void setupScriptShouldBeRunned() {
			int handler = setupDb.open();
			string path = Path.Combine(
					   Directory.GetCurrentDirectory(), "setup-Oracle.sql");
			setupDb.runFile(handler, "setup-Oracle.sql");
			setupDb.close(handler);
			Assert.IsTrue(true, "setup Script does not throw when runned");
		}

		[Test()]
		public void updateSellerKind() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "update sellerkind set rnd=rnd+1",
				["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param).Result;
			Dictionary<string, object> res = (Dictionary<string, object>)tRes;
			Assert.AreEqual(20, res["rowcount"]);

			setupDb.close(handler);
		}

		[Test()]
		public void selectFromCustomerKindShouldReturnDictionary() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "select * from customerkind",
				//["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param).Result;
			Assert.IsInstanceOf(typeof(List<object>), tRes, "query without callback should return  a List<object> ");
			List<object> res = (List<object>)tRes;
			Assert.AreEqual(1, res.Count, "select * from  CustomerKind should return 1 result set");
			Assert.IsInstanceOf(typeof(Dictionary<string, object>), res[0], "Result set is a dictionary<string,object>");
			Dictionary<string, object> resultSet = (Dictionary<string, object>)res[0];
			Assert.IsInstanceOf(typeof(Object[]), resultSet["meta"], "ResultSet.meta is a Object[] ");
			Assert.IsInstanceOf(typeof(List<object>), resultSet["rows"], "ResultSet.rows is a list<object> ");

			setupDb.close(handler);
		}

		[Test()]
		public void selectFromCustomerKindMetaShouldBeArrayOfColumnNames() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "select * from customerkind",
				//["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param).Result;

			Assert.IsInstanceOf(typeof(List<object>), tRes, "query without callback should return  a List<object[]> ");

			List<object> res = (List<object>)tRes;
			Dictionary<string, object> resultSet = (Dictionary<string, object>)res[0];
			Object[] meta = (Object[])resultSet["meta"];
			Assert.AreEqual("IDCUSTOMERKIND",meta[0] );
			Assert.AreEqual("NAME", meta[1]);
			Assert.AreEqual("RND", meta[2] );
			Assert.AreEqual(3, meta.Length);

			setupDb.close(handler);
		}

		[Test()]
		public void selectFromCustomerKindRowsShouldBeListOfObjects() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "select * from customerkind",
				//["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var t = ec.CompileFunc(param);
			var tRes = t.Result;
			Task.WaitAll(t);
			Assert.IsInstanceOf(typeof(List<object>), tRes, "query without callback should return a List<object[]> ");
			List<object> res = (List<object>)tRes;
			Dictionary<string, object> resultSet = (Dictionary<string, object>)res[0];
			List<object> rows = (List<object>)resultSet["rows"];
			Assert.AreEqual(40, rows.Count);
			for (int i = 0; i < 40; i++) {
				Object[] values = (Object[])rows[i];
				Assert.AreEqual(i * 3, values[0]);
				Assert.AreEqual("name" + (i * 3), values[1].ToString());
				Assert.IsInstanceOf(typeof(Double), values[2]);
			}
			setupDb.close(handler);
		}

		[Test()]
		public void twoSelectShouldReturnTwoResultSet() {
			int handler = setupDb.open();

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = @"DECLARE c1 SYS_REFCURSOR;
                                       c2 SYS_REFCURSOR;
                    BEGIN
                        open c1 for select * from customerkind FETCH NEXT 7 ROWS ONLY;
                        open c2 for select * from sellerkind FETCH NEXT 3 ROWS ONLY;
                        DBMS_SQL.RETURN_RESULT(c1);
                        DBMS_SQL.RETURN_RESULT(c2);
                    END;",
				//["cmd"] = "nonquery",
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var t = ec.CompileFunc(param);
			Task.WaitAll(t);
			Assert.IsInstanceOf(typeof(List<object>), t.Result, "query without callback should return  a List<object[]> ");
			List<object> res = (List<object>)t.Result;
			Assert.AreEqual(res.Count, 2, "query with two select should return two resultset");
			Assert.IsInstanceOf(typeof(Dictionary<string, object>), res[0], "Result set 1 is a dictionary<string,object>");
			Assert.IsInstanceOf(typeof(Dictionary<string, object>), res[1], "Result set 2 is a dictionary<string,object>");

			Dictionary<string, object> resultSet1 = (Dictionary<string, object>)res[0];
			Dictionary<string, object> resultSet2 = (Dictionary<string, object>)res[1];

			Assert.IsInstanceOf(typeof(Object[]), resultSet1["meta"], "ResultSet1.meta is a Object[] ");
			Assert.IsInstanceOf(typeof(List<object>), resultSet1["rows"], "ResultSet1.rows is a list<object> ");
			List<object> rows1 = (List<object>)resultSet1["rows"];
			Assert.AreEqual(7, rows1.Count, "ResultSet1.rows is a list<object> of 7 elements ");

			Assert.IsInstanceOf(typeof(Object[]), resultSet2["meta"], "ResultSet2.meta is a Object[] ");
			Assert.IsInstanceOf(typeof(List<object>), resultSet2["rows"], "ResultSet2.rows is a list<object> ");
			List<object> rows2 = (List<object>)resultSet2["rows"];
			Assert.AreEqual(3, rows2.Count, "ResultSet1.rows is a list<object> of 3 elements ");

			setupDb.close(handler);
		}

		[Test()]
		public void selectFromCustomerKindRowsWithCallBack() {
			int handler = setupDb.open();
			var resultSet = new List<object>();
			int nCount = 0;
			var callBack = new Func<object, Task<object>>((o) => {
				resultSet.Add(o);
				nCount++;
				return Task.FromResult<object>(null);
			});

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "select * from customerkind FETCH NEXT 6 ROWS ONLY",
				["callback"] = callBack,
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param);
			
			Task.WaitAll(tRes);
			Assert.IsInstanceOf(typeof(List<object>), tRes.Result, "query with callback should return a List<object[]> ");
			List<object> res = (List<object>)tRes.Result;
			Assert.AreEqual(2, nCount, "Callback should be called 2 times");
			Assert.IsAssignableFrom(typeof(Dictionary<string, object>), resultSet[0]);
			Assert.IsAssignableFrom(typeof(Dictionary<string, object>), resultSet[1]);

			Dictionary<string, object> resSet1 = (Dictionary<string, object>)resultSet[0];
			Dictionary<string, object> resSet2 = (Dictionary<string, object>)resultSet[1];

			Assert.IsTrue(resSet1.ContainsKey("meta"), "First resultset has meta");
			Assert.IsFalse(resSet2.ContainsKey("meta"), "Second resultset has no meta");

			Assert.IsTrue(resSet1.ContainsKey("rows"), "First resultset has rows");
			Assert.AreEqual(6, ((List<object>)resSet1["rows"]).Count, "First resultset has 6 rows");
			Assert.IsFalse(resSet2.ContainsKey("rows"), "Second resultset has no rows");

			Assert.IsFalse(resSet1.ContainsKey("resolve"), "First resultset has no resolve");
			Assert.IsTrue(resSet2.ContainsKey("resolve"), "Second resultset has resolve");

			setupDb.close(handler);
		}


		[Test()]
		public void selectFromCustomerKindRowsWithCallBackAndPacketSize() {
			int handler = setupDb.open();
			var resultSet = new List<object>();
			int nCount = 0;
			var callBack = new Func<object, Task<object>>((o) => {
				resultSet.Add(o);
				nCount++;
				return Task.FromResult<object>(null);
			});

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = "select * from customerkind",
				["callback"] = callBack,
				["packetSize"] = 5,
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param);			
			Task.WaitAll(tRes);
			Assert.IsInstanceOf(typeof(List<object>), tRes.Result, "query with callback should return  a List<object[]> ");
			List<object> res = (List<object>)tRes.Result;
			Assert.AreEqual(0, res.Count, "When a callback is present, no data is returned in the main result");

			Assert.AreEqual(10, nCount, "Callback should be called 10 times");
			for (int i = 0; i < 10; i++) {
				Dictionary<string, object> resSet = (Dictionary<string, object>)resultSet[i];
				if (i == 0) {
					Assert.IsTrue(resSet.ContainsKey("meta"), "First resultset has meta");
				}
				else {
					Assert.IsFalse(resSet.ContainsKey("meta"), "Subsequent resultsets have no meta");
				}

				if (i == 0 || i == 9) {
					Assert.IsFalse(resSet.ContainsKey("rows"), "First and last resultset have no rows");
				}
				else {
					Assert.IsTrue(resSet.ContainsKey("rows"), "Subsequent resultsets have rows");
					List<object> rows = (List<object>)resSet["rows"];
					Assert.AreEqual(rows.Count, 5);
					for (int j = 0; j < 5; j++) {
						Object[] values = (Object[])rows[j];
						Assert.AreEqual(((i - 1) * 5 + j) * 3, values[0]);
						Assert.AreEqual("name" + (((i - 1) * 5 + j) * 3), values[1].ToString());
						Assert.IsInstanceOf(typeof(Double), values[2]);
					}
				}

				if (i == 9) {
					Assert.IsTrue(resSet.ContainsKey("resolve"), "Last resultset has resolve");
				}
				else {
					Assert.IsFalse(resSet.ContainsKey("resolve"), "Other resultsets have no resolve");
				}
			}

			setupDb.close(handler);
		}

		[Test()]
		public void doubleSelectWithCallBackAndPacketSize() {
			int handler = setupDb.open();
			var resultSet = new List<object>();
			int nCount = 0;
			var callBack = new Func<object, Task<object>>((o) => {
				resultSet.Add(o);
				nCount++;
				return Task.FromResult<object>(null);
			});

			Dictionary<string, object> param = new Dictionary<string, object> {
				["source"] = @"DECLARE c1 SYS_REFCURSOR;
                                       c2 SYS_REFCURSOR;
                        BEGIN
                            open c1 for select * from customerkind;
                            open c2 for select * from sellerkind;
                            DBMS_SQL.RETURN_RESULT(c1);
                            DBMS_SQL.RETURN_RESULT(c2);
                        END;",
				["callback"] = callBack,
				["packetSize"] = 5,
				["handler"] = handler,
				["driver"] = setupDb.getDriver()
			};
			EdgeCompiler ec = new EdgeCompiler();
			var tRes = ec.CompileFunc(param);			
			Task.WaitAll(tRes);
			Assert.IsInstanceOf(typeof(List<object>), tRes.Result, "query with callback should return a List<object[]> ");
			List<object> res = (List<object>)tRes.Result;
			Assert.That(res.Count, Is.EqualTo(0), "When a callback is present, no data is returned in the main result");
			//9 times for customerkind (1meta + 40 rows=5*8) and  5 times for sellerkind (1 meta + 20 rows = 5*4) + 1 "resolve"
			Assert.That(nCount, Is.EqualTo(15), "Callback should be called 10 times");
			for (int i = 0; i < 15; i++) {
				Dictionary<string, object> resSet = (Dictionary<string, object>)resultSet[i];
				if (i == 0 || i == 9) {
					Assert.That(resSet.ContainsKey("meta"), Is.True, "First resultset has meta");
				}
				else {
					Assert.IsFalse(resSet.ContainsKey("meta"), "other resultsets have no meta");
				}

				if (i == 0 || i == 9 || i == 14) {
					Assert.IsFalse(resSet.ContainsKey("rows"), "Where there is meta o resolve there is no row");
				}	
				else {
					Assert.IsTrue(resSet.ContainsKey("rows"), "Subsequent resultsets have rows");
					List<object> rows = (List<object>)resSet["rows"];
					Assert.AreEqual(5, rows.Count);
					if (i < 9) {
						for (int j = 0; j < 5; j++) {
							Object[] values = (Object[])rows[j];
							Assert.That(values[0], Is.EqualTo(((i - 1) * 5 + j) * 3));
							Assert.That(values[1].ToString(), Is.EqualTo("name" + (((i - 1) * 5 + j) * 3)));
							Assert.IsInstanceOf(typeof(Double), values[2]);
						}
					}
					else {
						for (int j = 0; j < 5; j++) {
							Object[] values = (Object[])rows[j];
							Assert.AreEqual(((i - 10) * 5 + j) * 30, values[0]);
							Assert.AreEqual("name" + (((i - 10) * 5 + j) * 30), values[1].ToString());
							Assert.IsInstanceOf(typeof(Double), values[2]);
						}
					}
				}

				if (i == 14) {
					Assert.IsTrue(resSet.ContainsKey("resolve"), "Last resultset has resolve");
				}
				else {
					Assert.IsFalse(resSet.ContainsKey("resolve"), "Other resultsets have no resolve");
				}
			}

			setupDb.close(handler);
		}
	}
}

