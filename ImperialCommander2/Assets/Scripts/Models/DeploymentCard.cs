﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Saga;

public class DeploymentCard : IEquatable<DeploymentCard>
{
	//== data from JSON
	[JsonIgnore]
	public string name;
	public string id;
	public int tier;
	public string faction;
	public int priority;
	public int cost;
	public int rcost;
	public int size;
	public int fame;
	public int reimb;
	public string expansion;
	[JsonIgnore]
	public string ignored;
	public bool isElite;
	public bool isHero;
	[JsonIgnore]
	public string subname;
	public int health;
	public int speed;
	[JsonIgnore]
	public string[] traits;
	[JsonIgnore]
	public string[] surges;
	[JsonIgnore]
	public string[] keywords;
	[JsonIgnore]
	public GroupAbility[] abilities;
	public DiceColor[] defense;
	public DiceColor[] attacks;
	public AttackType attackType;
	public FigureSize miniSize;
	public GroupTraits[] groupTraits;
	public GroupTraits[] preferredTargets;
	//==

	//==upkeep properties
	public int currentSize;
	public int colorIndex;
	//[DefaultValue( false )]
	//[JsonProperty( DefaultValueHandling = DefaultValueHandling.Populate )]
	public bool hasActivated = false;
	public string bonusName, bonusText, rebelName;
	public InstructionOption instructionOption;
	public bool isDummy;
	public HeroState heroState;
	public bool isCustom = false;
	public string mugShotPath;
	public int[] woundTrackerValue = new int[] { 0, 0, 0 };
	//==end upkeep

	public bool Equals( DeploymentCard obj )
	{
		if ( obj == null )
			return false;
		DeploymentCard objAsPart = obj as DeploymentCard;
		if ( objAsPart == null )
			return false;
		else
			return id == objAsPart.id;
	}

	public static DeploymentCard CreateCustomCard( CustomEnemyDeployment ced )
	{
		var card = new DeploymentCard()
		{
			isCustom = true,
			isDummy = false,
			name = ced.enemyGroupData.cardName,
			id = ced.enemyGroupData.cardID,
			tier = 1,
			priority = 2,
			cost = ced.groupCost,
			rcost = ced.groupRedeployCost,
			size = ced.groupSize,
			fame = 6,
			reimb = 6,
			expansion = "Other",
			ignored = "",
			isElite = false,
			isHero = false,
			subname = "",
			health = ced.groupHealth,
			speed = ced.groupSpeed,
			surges = ced.surges.Split( '\n' ),
			keywords = ced.keywords.Split( '\n' ),
			abilities = new GroupAbility[0],
			defense = Utils.ParseCustomDice( ced.groupDefense.Split( ' ' ) ),
			attacks = Utils.ParseCustomDice( ced.groupAttack.Split( ' ' ) ),
			miniSize = FigureSize.Small1x1,
			traits = new string[0],//English version of groupTraits
			groupTraits = new GroupTraits[0],
			preferredTargets = ced.enemyGroupData.groupPriorityTraits.GetTraitArray(),
			attackType = ced.attackType
		};

		//health multiplier
		if ( ced.useThreatMultiplier )
			card.health *= DataStore.sagaSessionData.setupOptions.threatLevel;

		if ( !string.IsNullOrEmpty( ced.abilities.Trim() ) )
		{
			string[] alist = ced.abilities.Split( '\n' );
			var gaList = new List<GroupAbility>();
			foreach ( var item in alist )
			{
				string[] a = item.Split( ':' );
				if ( a.Length == 2 )
				{
					GroupAbility ab = new GroupAbility() { name = a[0], text = a[1] };
					gaList.Add( ab );
				}
				else
				{
					GroupAbility ab = new GroupAbility() { name = "", text = a[0] };
					gaList.Add( ab );
				}
			}
			card.abilities = gaList.ToArray();
		}

		if ( ced.customType == MarkerType.Rebel )
		{
			card.faction = "Mercenary";
		}
		else
		{
			card.faction = "Imperial";
		}

		if ( ced.iconType == MarkerType.Rebel )
			card.mugShotPath = $"Cards/Allies/{ced.thumbnailGroupRebel.Replace( "A", "M" )}";
		else
		{
			var dc = DataStore.GetEnemy( ced.thumbnailGroupImperial );
			card.mugShotPath = dc.mugShotPath;
		}

		if ( ced.enemyGroupData.useGenericMugshot )
		{
			if ( ced.iconType == MarkerType.Rebel )
				card.mugShotPath = "Cards/genericAlly";
			else
				card.mugShotPath = "Cards/genericEnemy";
		}

		return card;
	}
}

public class GroupAbility
{
	public string name;
	public string text;
}

public class CardLanguage
{
	public string id;
	public string name;
	public string subname;
	public string ignored;
	public string[] traits;
	public string[] surges;
	public string[] keywords;
	public GroupAbility[] abilities;
}

public class DCPointer
{
	public string name;
	public string id;
}
