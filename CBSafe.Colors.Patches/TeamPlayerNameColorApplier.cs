using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using Facepunch.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using ProtoBuf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class TeamPlayerNameColorApplier : MonoBehaviour
{
	public enum NameColorKind
	{
		Unknown,
		TeamPanel,
		Player,
		Ally,
		Enemy
	}

	private struct TextOrig
	{
		public TMP_Text Txt;

		public Color Color;
	}

	private struct UiTextOrig
	{
		public Text Txt;

		public Color Color;
	}

	private struct MemberOnlineColorOrig
	{
		public TeamMemberElement Element;

		public Color Online;
	}

	private struct PlayerWorldTargets
	{
		public PlayerNameTag LegacyTag;

		public ESPPlayerInfo EspInfo;

		public bool IsNeutral;

		public bool IsAlly;

		public bool IsEnemy;
	}

	private static readonly System.Collections.Generic.Dictionary<int, BasePlayer> _legacyTagPlayers = new System.Collections.Generic.Dictionary<int, BasePlayer>(64);

	private static readonly System.Collections.Generic.Dictionary<int, TextOrig> _originalTmpColors = new System.Collections.Generic.Dictionary<int, TextOrig>();

	private static readonly System.Collections.Generic.Dictionary<int, UiTextOrig> _originalUiColors = new System.Collections.Generic.Dictionary<int, UiTextOrig>();

	private static readonly System.Collections.Generic.Dictionary<int, MemberOnlineColorOrig> _originalOnlineColors = new System.Collections.Generic.Dictionary<int, MemberOnlineColorOrig>();

	private static readonly HashSet<ulong> _allyUserIds = new HashSet<ulong>();

	private static readonly HashSet<ulong> _enemyUserIds = new HashSet<ulong>();

	private static readonly System.Collections.Generic.Dictionary<ulong, PlayerWorldTargets> _worldTargets = new System.Collections.Generic.Dictionary<ulong, PlayerWorldTargets>(64);

	private static readonly System.Collections.Generic.List<ContactsEntry> _enemyContacts = new System.Collections.Generic.List<ContactsEntry>(16);

	private static bool _enemyContactsCached;

	private static bool _customActive;

	private static bool _hasTeamNameColor;

	private static bool _hasPlayerColor;

	private static bool _hasAllyColor;

	private static bool _hasEnemyColor;

	private static Color _teamNameColor;

	private static Color _playerColor;

	private static Color _allyColor;

	private static Color _enemyColor;

	public TeamPlayerNameColorApplier(IntPtr ptr)
		: base(ptr)
	{
	}

	public static void QuickApplyFromConfig(NameColorKind only = NameColorKind.Unknown)
	{
		try
		{
			RefreshCachedColors();
			if (!_customActive)
			{
				return;
			}
			if (only == NameColorKind.Unknown || only == NameColorKind.TeamPanel)
			{
				MarkTeamPanelDirty();
			}
			if (only == NameColorKind.Unknown || only == NameColorKind.Ally || only == NameColorKind.Enemy)
			{
				if (_worldTargets.Count == 0)
				{
					RebuildWorldCache(only);
				}
				ApplyWorldFromCache(only);
			}
			if (only == NameColorKind.Unknown || only == NameColorKind.Player)
			{
				if (only == NameColorKind.Unknown)
				{
					RebuildNeutralWorldCache();
				}
				ApplyWorldFromCache(NameColorKind.Player);
			}
			if ((only == NameColorKind.Unknown || only == NameColorKind.Enemy) && _hasEnemyColor)
			{
				EnsureEnemyContactsCached();
				ApplyEnemyContactsOnly();
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(51, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamPlayerNameColorApplier] QuickApplyFromConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public void ForceReapplyNow()
	{
		try
		{
			InvalidateAllCaches();
			QuickApplyFromConfig();
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(46, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamPlayerNameColorApplier] ForceReapplyNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	internal static void OnClientTeamPacketReceived()
	{
		try
		{
			HashSet<ulong> a = new HashSet<ulong>(_allyUserIds);
			UpdateAllyCacheFromClientTeam();
			if (!SetsEqual(a, _allyUserIds))
			{
				OnAllyMembershipChanged();
			}
		}
		catch
		{
		}
	}

	internal static void OnEnemyRelationshipsChanged(RelationshipManager.PlayerRelationships rels)
	{
		try
		{
			HashSet<ulong> a = new HashSet<ulong>(_enemyUserIds);
			UpdateEnemyCache(rels);
			if (!SetsEqual(a, _enemyUserIds))
			{
				OnEnemySetChanged();
			}
		}
		catch
		{
		}
	}

	private static void OnAllyMembershipChanged()
	{
		RefreshCachedColors();
		if (_customActive)
		{
			PruneWorldCacheForMissingAllies();
			RebuildWorldCache(NameColorKind.Ally);
			ApplyWorldFromCache(NameColorKind.Ally);
		}
	}

	private static void OnEnemySetChanged()
	{
		RefreshCachedColors();
		if (_customActive)
		{
			_enemyContactsCached = false;
			_enemyContacts.Clear();
			PruneWorldCacheForMissingEnemies();
			RebuildWorldCache(NameColorKind.Enemy);
			ApplyWorldFromCache(NameColorKind.Enemy);
			if (_hasEnemyColor)
			{
				EnsureEnemyContactsCached();
				ApplyEnemyContactsOnly();
			}
		}
	}

	internal static void OnSessionSceneChanged()
	{
		InvalidateAllCaches();
		QuickApplyFromConfig();
	}

	internal static void UpdateAllyCacheFromClientTeam()
	{
		_allyUserIds.Clear();
		BasePlayer localPlayer = GetLocalPlayer();
		if (localPlayer == null)
		{
			return;
		}
		ulong num = 0uL;
		try
		{
			num = localPlayer.userID;
		}
		catch
		{
			num = 0uL;
		}
		try
		{
			PlayerTeam clientTeam = localPlayer.clientTeam;
			if (clientTeam == null)
			{
				return;
			}
			Il2CppSystem.Collections.Generic.List<PlayerTeam.TeamMember> members = clientTeam.members;
			if (members == null)
			{
				return;
			}
			int count = members.Count;
			for (int i = 0; i < count; i++)
			{
				PlayerTeam.TeamMember teamMember = members[i];
				if (teamMember != null)
				{
					ulong num2 = 0uL;
					try
					{
						num2 = teamMember.userID;
					}
					catch
					{
						continue;
					}
					if (num2 != 0L && num2 != num)
					{
						_allyUserIds.Add(num2);
					}
				}
			}
		}
		catch
		{
		}
	}

	internal static void OnEspSetColor(ESPPlayerInfo info, ref Color color)
	{
		if (!(info == null) && !(color.a < 0.01f))
		{
			BasePlayer player = null;
			try
			{
				player = info.Entity;
			}
			catch
			{
			}
			if (TryGetWorldNameColorForEntity(player, out var color2))
			{
				TrackNeutralWorldTarget(player, info, null);
				float a = color.a;
				color = color2;
				color.a = a;
			}
		}
	}

	internal static bool IsEspNameplateVisible(ESPPlayerInfo info)
	{
		if (info == null)
		{
			return false;
		}
		try
		{
			CanvasGroup group = info.group;
			if (group != null && group.alpha < 0.01f)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (info.numFramesVisible <= 0)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	internal static void OnPlayerNameTagUpdateFrom(PlayerNameTag tag, BasePlayer player)
	{
		if (tag == null || player == null)
		{
			return;
		}
		try
		{
			int instanceID = tag.GetInstanceID();
			if (instanceID != 0)
			{
				_legacyTagPlayers[instanceID] = player;
			}
		}
		catch
		{
		}
		TrackNeutralWorldTarget(player, null, tag);
	}

	internal static void OnPlayerNameTagUpdateColor(PlayerNameTag tag)
	{
		if (tag == null || !RefreshCachedColorsIfNeeded())
		{
			return;
		}
		BasePlayer value = null;
		try
		{
			int instanceID = tag.GetInstanceID();
			if (instanceID != 0)
			{
				_legacyTagPlayers.TryGetValue(instanceID, out value);
			}
		}
		catch
		{
		}
		if (!(value == null) && TryGetWorldNameColorForEntity(value, out var color))
		{
			PaintPlayerNameTag(tag, color, preserveAlpha: true);
		}
	}

	private static bool RefreshCachedColorsIfNeeded()
	{
		RefreshCachedColors();
		if (_customActive)
		{
			if (!_hasPlayerColor && !_hasAllyColor)
			{
				return _hasEnemyColor;
			}
			return true;
		}
		return false;
	}

	internal static bool TryGetWorldNameColorForEntity(BasePlayer player, out Color color)
	{
		color = default(Color);
		if (player == null)
		{
			return false;
		}
		if (!RefreshCachedColorsIfNeeded())
		{
			return false;
		}
		ulong num = 0uL;
		ulong num2 = 0uL;
		try
		{
			num = player.userID;
			BasePlayer localPlayer = GetLocalPlayer();
			num2 = ((localPlayer != null) ? localPlayer.userID : 0);
		}
		catch
		{
			return false;
		}
		if (num == 0L || num == num2)
		{
			return false;
		}
		if (_hasAllyColor)
		{
			if (_allyUserIds.Count == 0)
			{
				UpdateAllyCacheFromClientTeam();
			}
			if (_allyUserIds.Contains(num))
			{
				color = _allyColor;
				return true;
			}
		}
		if (_hasEnemyColor && _enemyUserIds.Contains(num))
		{
			color = _enemyColor;
			return true;
		}
		if (_hasPlayerColor && !IsTaggedAllyOrEnemy(num))
		{
			color = _playerColor;
			return true;
		}
		return false;
	}

	private static bool IsTaggedAllyOrEnemy(ulong uid)
	{
		if (uid == 0L)
		{
			return false;
		}
		if (_hasAllyColor)
		{
			if (_allyUserIds.Count == 0)
			{
				UpdateAllyCacheFromClientTeam();
			}
			if (_allyUserIds.Contains(uid))
			{
				return true;
			}
		}
		if (_hasEnemyColor)
		{
			return _enemyUserIds.Contains(uid);
		}
		return false;
	}

	internal static void PrepareTeamMemberUpdateState(TeamMemberElement el, bool isOnline, bool isAlive, bool isWounded)
	{
		if (el == null)
		{
			return;
		}
		RefreshCachedColors();
		if (!_customActive || !_hasTeamNameColor || !isOnline || !isAlive || isWounded)
		{
			return;
		}
		int instanceID;
		try
		{
			instanceID = el.GetInstanceID();
		}
		catch
		{
			return;
		}
		if (instanceID != 0 && !_originalOnlineColors.ContainsKey(instanceID))
		{
			_originalOnlineColors[instanceID] = new MemberOnlineColorOrig
			{
				Element = el,
				Online = el.onlineColor
			};
		}
		try
		{
			el.onlineColor = _teamNameColor;
		}
		catch
		{
		}
	}

	internal static void UpdateEnemyCache(RelationshipManager.PlayerRelationships rels)
	{
		_enemyUserIds.Clear();
		if (rels == null)
		{
			return;
		}
		try
		{
			Il2CppSystem.Collections.Generic.Dictionary<ulong, RelationshipManager.PlayerRelationshipInfo> relations = rels.relations;
			if (relations == null)
			{
				return;
			}
			Il2CppSystem.Collections.Generic.Dictionary<ulong, RelationshipManager.PlayerRelationshipInfo>.Enumerator enumerator = relations.GetEnumerator();
			while (enumerator.MoveNext())
			{
				RelationshipManager.PlayerRelationshipInfo value = enumerator.Current.Value;
				if (value != null && value.type == RelationshipManager.RelationshipType.Enemy)
				{
					_enemyUserIds.Add(enumerator.Current.Key);
				}
			}
		}
		catch
		{
		}
	}

	private static void InvalidateAllCaches()
	{
		_worldTargets.Clear();
		_enemyContacts.Clear();
		_enemyContactsCached = false;
		_allyUserIds.Clear();
		EspNameplateBridge.InvalidateCanvasCache();
	}

	private static void RefreshCachedColors()
	{
		_customActive = PluginConfig.IsCustomActive();
		if (!_customActive)
		{
			_hasTeamNameColor = (_hasPlayerColor = (_hasAllyColor = (_hasEnemyColor = false)));
			return;
		}
		_hasTeamNameColor = PluginConfig.TryResolveTeamName(out _teamNameColor);
		_hasPlayerColor = PluginConfig.TryResolvePlayerName(out _playerColor);
		_hasAllyColor = PluginConfig.TryResolveAllyName(out _allyColor);
		_hasEnemyColor = PluginConfig.TryResolveEnemyName(out _enemyColor);
	}

	private static void MarkTeamPanelDirty()
	{
		try
		{
			TeamUI.dirty = true;
		}
		catch
		{
		}
	}

	private static void EnsureEnemyContactsCached()
	{
		if (!_enemyContactsCached)
		{
			RefreshEnemyContactsOnce();
			_enemyContactsCached = true;
		}
	}

	private static void RefreshEnemyContactsOnce()
	{
		_enemyContacts.Clear();
		if (!_hasEnemyColor)
		{
			return;
		}
		try
		{
			Il2CppArrayBase<ContactsEntry> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<ContactsEntry>();
			int num = il2CppArrayBase?.Length ?? 0;
			for (int i = 0; i < num; i++)
			{
				ContactsEntry contactsEntry = il2CppArrayBase[i];
				if (contactsEntry == null)
				{
					continue;
				}
				try
				{
					if (contactsEntry.info != null && contactsEntry.info.type == RelationshipManager.RelationshipType.Enemy)
					{
						_enemyContacts.Add(contactsEntry);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void RebuildNeutralWorldCache()
	{
		if (!_hasPlayerColor)
		{
			return;
		}
		System.Collections.Generic.List<ulong> list = new System.Collections.Generic.List<ulong>(16);
		foreach (System.Collections.Generic.KeyValuePair<ulong, PlayerWorldTargets> worldTarget in _worldTargets)
		{
			if (worldTarget.Value.IsNeutral)
			{
				list.Add(worldTarget.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_worldTargets.Remove(list[i]);
		}
		BasePlayer localPlayer = GetLocalPlayer();
		ulong num = 0uL;
		if (localPlayer != null)
		{
			try
			{
				num = localPlayer.userID;
			}
			catch
			{
				num = 0uL;
			}
		}
		try
		{
			ESPCanvas eSPCanvas = EspNameplateBridge.TryGetCanvas();
			if (eSPCanvas == null)
			{
				return;
			}
			ESPPlayerInfo[] array = eSPCanvas.Elements;
			if (array == null)
			{
				return;
			}
			ESPPlayerInfo[] array2 = array;
			foreach (ESPPlayerInfo eSPPlayerInfo in array2)
			{
				if (eSPPlayerInfo == null)
				{
					continue;
				}
				BasePlayer basePlayer = null;
				try
				{
					basePlayer = eSPPlayerInfo.Entity;
				}
				catch
				{
				}
				if (basePlayer == null)
				{
					continue;
				}
				ulong num2 = 0uL;
				try
				{
					num2 = basePlayer.userID;
				}
				catch
				{
					continue;
				}
				if (num2 != 0L && num2 != num && !IsTaggedAllyOrEnemy(num2))
				{
					PlayerNameTag playerNameTag = null;
					try
					{
						playerNameTag = basePlayer.GetComponentInChildren<PlayerNameTag>(includeInactive: true);
					}
					catch
					{
					}
					RegisterNeutralTarget(num2, eSPPlayerInfo, playerNameTag);
				}
			}
		}
		catch
		{
		}
	}

	private static void TrackNeutralWorldTarget(BasePlayer player, ESPPlayerInfo esp, PlayerNameTag tag)
	{
		if (!_hasPlayerColor || player == null)
		{
			return;
		}
		ulong num = 0uL;
		try
		{
			num = player.userID;
		}
		catch
		{
			return;
		}
		if (num != 0L)
		{
			ulong num2 = 0uL;
			try
			{
				BasePlayer localPlayer = GetLocalPlayer();
				num2 = ((localPlayer != null) ? localPlayer.userID : 0);
			}
			catch
			{
				num2 = 0uL;
			}
			if (num != num2 && !IsTaggedAllyOrEnemy(num))
			{
				RegisterNeutralTarget(num, esp, tag);
			}
		}
	}

	private static void RegisterNeutralTarget(ulong uid, ESPPlayerInfo esp, PlayerNameTag tag)
	{
		if (uid != 0L && (!(esp == null) || !(tag == null)))
		{
			if (!_worldTargets.TryGetValue(uid, out var value))
			{
				value = default(PlayerWorldTargets);
			}
			if (esp != null)
			{
				value.EspInfo = esp;
			}
			if (tag != null)
			{
				value.LegacyTag = tag;
			}
			value.IsNeutral = true;
			value.IsAlly = false;
			value.IsEnemy = false;
			_worldTargets[uid] = value;
		}
	}

	private static void RebuildWorldCache(NameColorKind only)
	{
		if (!_hasAllyColor && !_hasEnemyColor)
		{
			return;
		}
		if (_hasAllyColor && _allyUserIds.Count == 0)
		{
			UpdateAllyCacheFromClientTeam();
		}
		BasePlayer localPlayer = GetLocalPlayer();
		ulong localTeam = 0uL;
		ulong localId = 0uL;
		if (localPlayer != null)
		{
			try
			{
				localTeam = localPlayer.currentTeam;
				localId = localPlayer.userID;
			}
			catch
			{
				localTeam = 0uL;
				localId = 0uL;
			}
		}
		if (only != 0)
		{
			System.Collections.Generic.List<ulong> list = new System.Collections.Generic.List<ulong>(8);
			foreach (System.Collections.Generic.KeyValuePair<ulong, PlayerWorldTargets> worldTarget in _worldTargets)
			{
				if (only == NameColorKind.Ally && !worldTarget.Value.IsAlly)
				{
					list.Add(worldTarget.Key);
				}
				else if (only == NameColorKind.Enemy && !worldTarget.Value.IsEnemy)
				{
					list.Add(worldTarget.Key);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				_worldTargets.Remove(list[i]);
			}
		}
		if (_hasAllyColor && _allyUserIds.Count > 0)
		{
			foreach (ulong allyUserId in _allyUserIds)
			{
				BasePlayer basePlayer = ClientPlayerLookupBridge.FindByID(allyUserId);
				if (basePlayer == null)
				{
					continue;
				}
				try
				{
					if (basePlayer.IsDestroyed)
					{
						continue;
					}
				}
				catch
				{
					continue;
				}
				TryAddPlayerToWorldCache(basePlayer, localId, localTeam, only);
			}
		}
		if (!_hasEnemyColor || _enemyUserIds.Count <= 0)
		{
			return;
		}
		foreach (ulong enemyUserId in _enemyUserIds)
		{
			BasePlayer basePlayer2 = ClientPlayerLookupBridge.FindByID(enemyUserId);
			if (basePlayer2 == null)
			{
				continue;
			}
			try
			{
				if (basePlayer2.IsDestroyed)
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			TryAddPlayerToWorldCache(basePlayer2, localId, localTeam, only);
		}
	}

	private static void TryAddPlayerToWorldCache(BasePlayer bp, ulong localId, ulong localTeam, NameColorKind only)
	{
		if (bp == null)
		{
			return;
		}
		ulong num = 0uL;
		try
		{
			num = bp.userID;
			_ = bp.currentTeam;
		}
		catch
		{
			return;
		}
		if (num == 0L || num == localId)
		{
			return;
		}
		if (_hasAllyColor && _allyUserIds.Count == 0)
		{
			UpdateAllyCacheFromClientTeam();
		}
		bool flag = _hasAllyColor && _allyUserIds.Contains(num);
		bool flag2 = _hasEnemyColor && _enemyUserIds.Contains(num);
		if ((!flag && !flag2) || (only == NameColorKind.Ally && !flag) || (only == NameColorKind.Enemy && !flag2))
		{
			return;
		}
		PlayerNameTag playerNameTag = null;
		try
		{
			playerNameTag = bp.GetComponentInChildren<PlayerNameTag>(includeInactive: true);
		}
		catch
		{
		}
		ESPPlayerInfo eSPPlayerInfo = null;
		try
		{
			eSPPlayerInfo = EspNameplateBridge.FindOnPlayer(bp);
		}
		catch
		{
		}
		if (!(playerNameTag == null) || !(eSPPlayerInfo == null))
		{
			if (!_worldTargets.TryGetValue(num, out var value))
			{
				value = default(PlayerWorldTargets);
			}
			if (playerNameTag != null)
			{
				value.LegacyTag = playerNameTag;
			}
			if (eSPPlayerInfo != null)
			{
				value.EspInfo = eSPPlayerInfo;
			}
			value.IsNeutral = false;
			value.IsAlly = flag;
			value.IsEnemy = flag2;
			_worldTargets[num] = value;
		}
	}

	private static void ApplyWorldFromCache(NameColorKind only)
	{
		if (!_hasPlayerColor && !_hasAllyColor && !_hasEnemyColor)
		{
			return;
		}
		foreach (System.Collections.Generic.KeyValuePair<ulong, PlayerWorldTargets> worldTarget in _worldTargets)
		{
			PlayerWorldTargets value = worldTarget.Value;
			if (value.LegacyTag == null && value.EspInfo == null)
			{
				continue;
			}
			switch (only)
			{
			case NameColorKind.Player:
				if (value.IsNeutral && _hasPlayerColor)
				{
					if (value.LegacyTag != null)
					{
						PaintPlayerNameTag(value.LegacyTag, _playerColor, preserveAlpha: true);
					}
					if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
					{
						EspNameplateBridge.Paint(value.EspInfo, _playerColor);
					}
				}
				continue;
			case NameColorKind.Ally:
				if (value.IsAlly)
				{
					if (value.LegacyTag != null)
					{
						PaintPlayerNameTag(value.LegacyTag, _allyColor, preserveAlpha: true);
					}
					if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
					{
						EspNameplateBridge.Paint(value.EspInfo, _allyColor);
					}
				}
				continue;
			case NameColorKind.Enemy:
				if (value.IsEnemy)
				{
					if (value.LegacyTag != null)
					{
						PaintPlayerNameTag(value.LegacyTag, _enemyColor, preserveAlpha: true);
					}
					if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
					{
						EspNameplateBridge.Paint(value.EspInfo, _enemyColor);
					}
				}
				continue;
			}
			if (value.IsAlly)
			{
				if (value.LegacyTag != null)
				{
					PaintPlayerNameTag(value.LegacyTag, _allyColor, preserveAlpha: true);
				}
				if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
				{
					EspNameplateBridge.Paint(value.EspInfo, _allyColor);
				}
			}
			else if (value.IsEnemy)
			{
				if (value.LegacyTag != null)
				{
					PaintPlayerNameTag(value.LegacyTag, _enemyColor, preserveAlpha: true);
				}
				if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
				{
					EspNameplateBridge.Paint(value.EspInfo, _enemyColor);
				}
			}
			else if (value.IsNeutral && _hasPlayerColor)
			{
				if (value.LegacyTag != null)
				{
					PaintPlayerNameTag(value.LegacyTag, _playerColor, preserveAlpha: true);
				}
				if (value.EspInfo != null && IsEspNameplateVisible(value.EspInfo))
				{
					EspNameplateBridge.Paint(value.EspInfo, _playerColor);
				}
			}
		}
	}

	private static void ApplyEnemyContactsOnly()
	{
		if (!_customActive || !_hasEnemyColor)
		{
			return;
		}
		for (int i = 0; i < _enemyContacts.Count; i++)
		{
			ContactsEntry contactsEntry = _enemyContacts[i];
			if (!(((contactsEntry != null) ? contactsEntry.nameText : null) == null))
			{
				PaintTmp(contactsEntry.nameText, _enemyColor);
			}
		}
	}

	internal static void PaintTmpShared(TMP_Text txt, Color color)
	{
		PaintTmp(txt, color);
	}

	private static void PaintPlayerNameTag(PlayerNameTag tag, Color color, bool preserveAlpha = false)
	{
		if (tag == null)
		{
			return;
		}
		Text text = null;
		try
		{
			text = tag.text;
		}
		catch
		{
		}
		if (text != null)
		{
			Color color2 = color;
			if (preserveAlpha)
			{
				try
				{
					color2.a = text.color.a;
				}
				catch
				{
				}
			}
			PaintUiText(text, color2);
		}
		try
		{
			Gradient color3 = tag.color;
			if (color3 == null)
			{
				return;
			}
			float alpha = color.a;
			if (preserveAlpha)
			{
				try
				{
					GradientAlphaKey[] array = color3.alphaKeys;
					if (array != null && array.Length != 0)
					{
						alpha = array[0].alpha;
					}
				}
				catch
				{
				}
			}
			GradientColorKey[] array2 = new GradientColorKey[2];
			array2[0].color = color;
			array2[0].time = 0f;
			array2[1].color = color;
			array2[1].time = 1f;
			GradientAlphaKey[] array3 = new GradientAlphaKey[2];
			array3[0].alpha = alpha;
			array3[0].time = 0f;
			array3[1].alpha = alpha;
			array3[1].time = 1f;
			color3.SetKeys(array2, array3);
		}
		catch
		{
		}
	}

	private static void PaintTmp(TMP_Text txt, Color color)
	{
		if (txt == null)
		{
			return;
		}
		int instanceID;
		try
		{
			instanceID = txt.GetInstanceID();
		}
		catch
		{
			return;
		}
		if (!_originalTmpColors.ContainsKey(instanceID))
		{
			_originalTmpColors[instanceID] = new TextOrig
			{
				Txt = txt,
				Color = txt.color
			};
		}
		try
		{
			txt.color = color;
		}
		catch
		{
		}
	}

	private static void PaintUiText(Text txt, Color color)
	{
		if (txt == null)
		{
			return;
		}
		int instanceID;
		try
		{
			instanceID = txt.GetInstanceID();
		}
		catch
		{
			return;
		}
		if (!_originalUiColors.ContainsKey(instanceID))
		{
			_originalUiColors[instanceID] = new UiTextOrig
			{
				Txt = txt,
				Color = txt.color
			};
		}
		try
		{
			txt.color = color;
		}
		catch
		{
		}
	}

	private static void PruneWorldCacheForMissingAllies()
	{
		if (_worldTargets.Count == 0)
		{
			return;
		}
		System.Collections.Generic.List<ulong> list = new System.Collections.Generic.List<ulong>(8);
		foreach (System.Collections.Generic.KeyValuePair<ulong, PlayerWorldTargets> worldTarget in _worldTargets)
		{
			if (worldTarget.Value.IsAlly && !_allyUserIds.Contains(worldTarget.Key))
			{
				list.Add(worldTarget.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_worldTargets.Remove(list[i]);
		}
	}

	private static void PruneWorldCacheForMissingEnemies()
	{
		if (_worldTargets.Count == 0)
		{
			return;
		}
		System.Collections.Generic.List<ulong> list = new System.Collections.Generic.List<ulong>(8);
		foreach (System.Collections.Generic.KeyValuePair<ulong, PlayerWorldTargets> worldTarget in _worldTargets)
		{
			if (worldTarget.Value.IsEnemy && !_enemyUserIds.Contains(worldTarget.Key))
			{
				list.Add(worldTarget.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_worldTargets.Remove(list[i]);
		}
	}

	private static bool SetsEqual(HashSet<ulong> a, HashSet<ulong> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		foreach (ulong item in a)
		{
			if (!b.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	private static BasePlayer GetLocalPlayer()
	{
		try
		{
			return LocalPlayer.Entity;
		}
		catch
		{
			return null;
		}
	}

	public static void RestoreOriginalsNow()
	{
		try
		{
			foreach (System.Collections.Generic.KeyValuePair<int, TextOrig> originalTmpColor in _originalTmpColors)
			{
				TMP_Text txt = originalTmpColor.Value.Txt;
				if (!(txt == null))
				{
					try
					{
						txt.color = originalTmpColor.Value.Color;
					}
					catch
					{
					}
				}
			}
			foreach (System.Collections.Generic.KeyValuePair<int, UiTextOrig> originalUiColor in _originalUiColors)
			{
				Text txt2 = originalUiColor.Value.Txt;
				if (!(txt2 == null))
				{
					try
					{
						txt2.color = originalUiColor.Value.Color;
					}
					catch
					{
					}
				}
			}
			foreach (System.Collections.Generic.KeyValuePair<int, MemberOnlineColorOrig> originalOnlineColor in _originalOnlineColors)
			{
				MemberOnlineColorOrig value = originalOnlineColor.Value;
				if (!(value.Element == null))
				{
					try
					{
						value.Element.onlineColor = value.Online;
					}
					catch
					{
					}
				}
			}
			_customActive = false;
			_hasTeamNameColor = (_hasPlayerColor = (_hasAllyColor = (_hasEnemyColor = false)));
			MarkTeamPanelDirty();
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(50, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamPlayerNameColorApplier] RestoreOriginalsNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
