﻿using System;
using System.Collections.Generic;

namespace Claw.Modules
{
	/// <summary>
	/// Descreve um módulo base.
	/// </summary>
	public abstract class BaseModule
	{
		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled != value)
				{
					_enabled = value;

					EnabledChanged?.Invoke(this);
				}
			}
		}
		public bool Exists { get; internal set; } = false;
		public bool DontDestroy = false;
		public string Name = string.Empty;
		public Game Game => Game.Instance;
		public readonly Transform Transform;
		private bool _enabled = true;
		internal List<string> tags = new List<string>();

		public event Action<BaseModule> EnabledChanged;

		public BaseModule(bool instantlyAdd)
		{
			Transform = new Transform(this);

			if (instantlyAdd) Game.Modules.Add(this);
		}

		/// <summary>
		/// É executado quando o módulo é adicionado ao jogo.
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Adiciona uma tag no módulo.
		/// </summary>
		/// <param name="tag">Case insensitive.</param>
		public void AddTag(string tag)
		{
			if (tag.Length == 0) return;

			tag = tag.ToLower();

			TagManager.AddModule(tag, this);

			if (!tags.Contains(tag)) tags.Add(tag);
		}
		/// <summary>
		/// Remove uma tag do módulo.
		/// </summary>
		/// <param name="tag">Case insensitive.</param>
		public void RemoveTag(string tag)
		{
			if (tag.Length == 0) return;

			tag = tag.ToLower();

			TagManager.RemoveModule(tag, this);

			if (tags.Contains(tag)) tags.Remove(tag);
		}
		/// <summary>
		/// Diz se este módulo possui uma tag específica.
		/// </summary>
		public bool HasTag(string tag) => tags.Contains(tag.ToLower());

		/// <summary>
		/// Destrói um módulo.
		/// </summary>
		protected static void Destroy(BaseModule module, bool runDestroy = true) => module.SelfDestroy(runDestroy);
		/// <summary>
		/// Destrói este módulo.
		/// </summary>
		public void SelfDestroy(bool runDestroy = true)
		{
			Transform.Parent = null;

			if (Transform.children.Count > 0)
			{
				for (int i = Transform.children.Count - 1; i >= 0; i--) Transform.children[i].Module.SelfDestroy(runDestroy);
			}

			Game.Modules.Remove(this);

			for (int i = 0; i < tags.Count; i++) TagManager.RemoveModule(tags[i], this);

			tags.Clear();

			if (runDestroy) OnDestroy();
		}
		/// <summary>
		/// Chamado quando o objeto é destruído.
		/// </summary>
		protected virtual void OnDestroy() { }
	}
}