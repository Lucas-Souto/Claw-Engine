﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Claw.Utils;

namespace Claw.Save
{
    /// <summary>
    /// Funções prontas para lidar com um save.
    /// </summary>
    public static class Save
	{
		public static SaveCrypt CryptFunction = DefaultCrypt;

        private static bool useCrypt = true, changed = false;
        private static string path = string.Empty;
        private static Sections save;
        private static Dictionary<ISaveValue, object> references;

        /// <summary>
        /// Abre o save.
        /// </summary>
        public static void Open(string path, bool useCrypt = true)
        {
            Save.path = path;
            Save.useCrypt = useCrypt;
            changed = false;
            references = new Dictionary<ISaveValue, object>();
            string content = string.Empty;

            if (!File.Exists(path)) File.Create(path).Close();
            else content = File.ReadAllText(path);

            if (content.Length > 0)
            {
                if (useCrypt) content = CryptFunction(content, false);

                save = SaveConvert.Deserialize(content);
            }

            if (save == null) save = new Sections();
        }

        /// <summary>
        /// Escreve numa seção do save.
        /// </summary>
        public static void Write(string section, string key, object value)
        {
            if (save != null)
            {
                if (!SectionExists(section)) save.Add(section, new Keys());

                if (!save.TryGetValue(section, out Keys keys))
                {
                    keys = new Keys();

                    save.Add(section, keys);
                }

                keys[key] = value;
                changed = true;
            }
            else throw new Exception("O save não está aberto!");
        }

        /// <summary>
        /// Lê uma chave de uma seção do save.
        /// </summary>
        public static T Read<T>(string section, string key, T defaultValue)
        {
            if (save != null)
            {
                if (save.TryGetValue(section, out Keys keys))
                {
                    if (keys.TryGetValue(key, out object value))
                    {
                        Type type = typeof(T);

                        if (value is ISaveValue saveValue) return (T)saveValue.Cast(type, references);
                        else if (type.IsEnum) return (T)Enum.Parse(type, value.ToString());
                        else if (type == typeof(bool)) return (T)Convert.ChangeType(value, typeof(bool));

                        return (T)value;
                    }
                }

                return defaultValue;
            }
            else throw new Exception("O save não está aberto!");
        }

        /// <summary>
        /// Limpa o save.
        /// </summary>
        public static void Clear()
        {
            if (path.Length > 0) save.Clear();
            else throw new Exception("O save não está aberto!");
        }

        /// <summary>
        /// Remove uma seção inteira do save.
        /// </summary>
        public static void RemoveSection(string section)
        {
            if (save != null) save.Remove(section);
            else throw new Exception("O save não está aberto!");
        }

        /// <summary>
        /// Remove uma chave de uma seção do save.
        /// </summary>
        public static void RemoveKey(string section, string key)
        {
            if (save != null) save[section].Remove(key);
            else throw new Exception("O save não está aberto!");
        }

        /// <summary>
        /// Verifica se a seção existe.
        /// </summary>
        public static bool SectionExists(string section) => save.Keys.Contains(section);

        /// <summary>
        /// Verifica se a chave existe.
        /// </summary>
        public static bool KeyExists(string section, string key)
        {
            if (!SectionExists(section)) return false;
            else return save[section].Keys.Contains(key);
        }

        /// <summary>
        /// Fecha o save.
        /// </summary>
        public static void Close()
        {
            if (changed)
            {
                string content = SaveConvert.Serialize(save);

                if (useCrypt) content = CryptFunction(content, true);

                if (path.Length > 0) File.WriteAllText(path, content);
            }

            save = null;
            references = null;
            path = string.Empty;
        }

		/// <summary>
		/// Descreve a base para a <see cref="CryptFunction"/>.
		/// </summary>
		/// <param name="input">O texto texto inicial.</param>
		/// <param name="encrypt">
		/// <para>Se verdadeiro, o <paramref name="input"/> deverá ser criptografado.</para>
		/// <para>Se falso, o <see cref="Input"/> deverá ser descriptografado.</para>
		/// </param>
		/// <returns>O texto após a operação.</returns>
		public delegate string SaveCrypt(string input, bool encrypt);
        /// <summary>
        /// Função padrão usada em <see cref="CryptFunction"/>.
        /// </summary>
        public static string DefaultCrypt(string input, bool encrypt)
        {
			if (encrypt) return StringCrypt.StringToBinary(StringCrypt.StringToHex(StringCrypt.Crypt(input, encrypt, 10)));

			return StringCrypt.Crypt(StringCrypt.HexToString(StringCrypt.BinaryToString(input)), encrypt, 10);
		}
	}
}