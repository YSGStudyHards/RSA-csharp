﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace com.github.xiangyuecn.rsacsharp {
	/// <summary>
	/// RSA操作类，只支持.NET Core环境，可跨平台使用。如需在.NET Framework中使用，请用RSA_ForWindows（也支持.NET Core，但只能Windows系统中使用）
	/// GitHub: https://github.com/xiangyuecn/RSA-csharp
	/// </summary>
	public class RSA_ForCore {
		/// <summary>
		/// 导出XML格式密钥对，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public string ToXML(bool convertToPublic = false) {
			return ToPEM(convertToPublic).ToXML(convertToPublic);
		}
		/// <summary>
		/// 将密钥对导出成PEM对象，如果convertToPublic含私钥的RSA将只返回公钥，仅含公钥的RSA不受影响
		/// </summary>
		public RSA_PEM ToPEM(bool convertToPublic = false) {
			return new RSA_PEM(rsa, convertToPublic);
		}




		/// <summary>
		/// 加密字符串（utf-8），出错抛异常
		/// </summary>
		public string Encode(string str) {
			return Convert.ToBase64String(Encode(Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 加密数据，出错抛异常
		/// </summary>
		public byte[] Encode(byte[] data) {
			int blockLen = rsa.KeySize / 8 - 11;
			if (data.Length <= blockLen) {
				return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
			}

			using (var dataStream = new MemoryStream(data))
			using (var enStream = new MemoryStream()) {
				Byte[] buffer = new Byte[blockLen];
				int len = dataStream.Read(buffer, 0, blockLen);

				while (len > 0) {
					Byte[] block = new Byte[len];
					Array.Copy(buffer, 0, block, 0, len);

					Byte[] enBlock = rsa.Encrypt(block, RSAEncryptionPadding.Pkcs1);
					enStream.Write(enBlock, 0, enBlock.Length);

					len = dataStream.Read(buffer, 0, blockLen);
				}

				return enStream.ToArray();
			}
		}
		/// <summary>
		/// 解密字符串（utf-8），解密异常返回null
		/// </summary>
		public string DecodeOrNull(string str) {
			if (String.IsNullOrEmpty(str)) {
				return null;
			}
			byte[] byts = null;
			try { byts = Convert.FromBase64String(str); } catch { }
			if (byts == null) {
				return null;
			}
			var val = DecodeOrNull(byts);
			if (val == null) {
				return null;
			}
			return Encoding.UTF8.GetString(val);
		}
		/// <summary>
		/// 解密数据，解密异常返回null
		/// </summary>
		public byte[] DecodeOrNull(byte[] data) {
			try {
				int blockLen = rsa.KeySize / 8;
				if (data.Length <= blockLen) {
					return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
				}

				using (var dataStream = new MemoryStream(data))
				using (var deStream = new MemoryStream()) {
					Byte[] buffer = new Byte[blockLen];
					int len = dataStream.Read(buffer, 0, blockLen);

					while (len > 0) {
						Byte[] block = new Byte[len];
						Array.Copy(buffer, 0, block, 0, len);

						Byte[] deBlock = rsa.Decrypt(block, RSAEncryptionPadding.Pkcs1);
						deStream.Write(deBlock, 0, deBlock.Length);

						len = dataStream.Read(buffer, 0, blockLen);
					}

					return deStream.ToArray();
				}
			} catch {
				return null;
			}
		}
		/// <summary>
		/// 对str进行签名，并指定hash算法（如：SHA256）
		/// </summary>
		public string Sign(string hash, string str) {
			return Convert.ToBase64String(Sign(hash, Encoding.UTF8.GetBytes(str)));
		}
		/// <summary>
		/// 对data进行签名，并指定hash算法（如：SHA256）
		/// </summary>
		public byte[] Sign(string hash, byte[] data) {
			return rsa.SignData(data, new HashAlgorithmName(hash), RSASignaturePadding.Pkcs1);
		}
		/// <summary>
		/// 验证字符串str的签名是否是sign，并指定hash算法（如：SHA256）
		/// </summary>
		public bool Verify(string hash, string sign, string str) {
			byte[] byts = null;
			try { byts = Convert.FromBase64String(sign); } catch { }
			if (byts == null) {
				return false;
			}
			return Verify(hash, byts, Encoding.UTF8.GetBytes(str));
		}
		/// <summary>
		/// 验证data的签名是否是sign，并指定hash算法（如：SHA256）
		/// </summary>
		public bool Verify(string hash, byte[] sign, byte[] data) {
			try {
				return rsa.VerifyData(data, sign, new HashAlgorithmName(hash), RSASignaturePadding.Pkcs1);
			} catch {
				return false;
			}
		}




		/// <summary>
		/// 最底层的RSA对象
		/// </summary>
		public RSA RSAObject {
			get {
				return rsa;
			}
		}

		/// <summary>
		/// 密钥位数
		/// </summary>
		public int KeySize {
			get {
				return rsa.KeySize;
			}
		}
		/// <summary>
		/// 是否包含私钥
		/// </summary>
		public bool HasPrivate {
			get {
				return ToPEM().HasPrivate;
			}
		}

		/// <summary>
		/// 用指定密钥大小创建一个新的RSA，会生成新密钥，出错抛异常
		/// </summary>
		public RSA_ForCore(int keySize) {
			rsa = RSA.Create();
			rsa.KeySize = keySize;
		}
		/// <summary>
		/// 通过指定的pem文件密钥或xml字符串密钥，创建一个RSA，pem或xml内可以只包含一个公钥或私钥，或都包含，出错抛异常
		/// </summary>
		public RSA_ForCore(string pemOrXML) {
			if (pemOrXML.Trim().StartsWith("<")) {
				rsa = RSA_PEM.FromXML(pemOrXML).GetRSA_ForCore();
			} else {
				rsa = RSA_PEM.FromPEM(pemOrXML).GetRSA_ForCore();
			}
		}
		/// <summary>
		/// 通过一个pem对象创建RSA，pem为公钥或私钥，出错抛异常
		/// </summary>
		public RSA_ForCore(RSA_PEM pem) {
			rsa = pem.GetRSA_ForCore();
		}
		/// <summary>
		/// 本方法会先生成RSA_PEM再创建RSA：通过公钥指数和私钥指数构造一个PEM，会反推计算出P、Q但和原始生成密钥的P、Q极小可能相同
		/// 注意：所有参数首字节如果是0，必须先去掉
		/// 出错将会抛出异常
		/// </summary>
		/// <param name="modulus">必须提供模数</param>
		/// <param name="exponent">必须提供公钥指数</param>
		/// <param name="dOrNull">私钥指数可以不提供，导出的PEM就只包含公钥</param>
		public RSA_ForCore(byte[] modulus, byte[] exponent, byte[] dOrNull) {
			rsa = new RSA_PEM(modulus, exponent, dOrNull).GetRSA_ForCore();
		}
		/// <summary>
		/// 本方法会先生成RSA_PEM再创建RSA：通过全量的PEM字段数据构造一个PEM，除了模数modulus和公钥指数exponent必须提供外，其他私钥指数信息要么全部提供，要么全部不提供（导出的PEM就只包含公钥）
		/// 注意：所有参数首字节如果是0，必须先去掉
		/// </summary>
		public RSA_ForCore(byte[] modulus, byte[] exponent, byte[] d, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] inverseQ) {
			rsa = new RSA_PEM(modulus, exponent, d, p, q, dp, dq, inverseQ).GetRSA_ForCore();
		}



		//.NET Framework 兼容编译
#if (NETCOREAPP || NETSTANDARD || NET) //https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/preprocessor-directives
		private RSA rsa;
		static public bool IS_CORE = true;
#else
		private dynamic rsa;
		static public bool IS_CORE = false;
		class RSAEncryptionPadding { static public object Pkcs1; }
		class RSASignaturePadding { static public object Pkcs1; }
		class HashAlgorithmName { public HashAlgorithmName(string _) { } }
#endif
	}
}
