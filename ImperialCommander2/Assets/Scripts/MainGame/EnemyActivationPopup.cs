﻿using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Saga;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyActivationPopup : MonoBehaviour
{
	public Image fader;
	public CanvasGroup cg;
	public TextMeshProUGUI bonusNameText, bonusText, ignoreText, modText;
	public Text enemyName, continueText;
	public Image thumbnail, colorPip;
	public DynamicCardPrefab cardPrefab;
	public DiceRoller diceRoller;
	public GameObject modifierBox;

	CardInstruction cardInstruction;
	DeploymentCard cardDescriptor;
	string rebel1;
	bool spaceListen;
	Action callback;

	public void Show( DeploymentCard cd, Difficulty difficulty, Action cb = null )
	{
		EventSystem.current.SetSelectedGameObject( null );
		//Debug.Log( "Showing: " + cd.name + " / " + cd.id );
		//clear values
		callback = cb;
		thumbnail.color = new Color( 1, 1, 1, 0 );
		bonusNameText.text = "";
		bonusText.text = "";
		enemyName.text = "";
		ignoreText.text = "";
		spaceListen = true;
		colorPip.color = DataStore.pipColors[cd.colorIndex].ToColor();
		continueText.text = DataStore.uiLanguage.uiMainApp.continueBtn;

		cardDescriptor = cd;

		cardInstruction = DataStore.activationInstructions.Where( x => x.instID == cd.id ).FirstOr( null );
		if ( cardInstruction == null )
		{
			Debug.Log( "cardInstruction is NULL: " + cd.id );
			GlowEngine.FindUnityObject<QuickMessage>().Show( "EnemyActivationPopup: cardInstruction is NULL: " + cd.id );
			return;
		}

		cardPrefab.InitCard( cd );

		//== no longer an issue
		//if ( cardInstruction == null )
		//{
		//	//not all elites have their own instruction, resulting in null found, so get its regular version instruction set by name instead
		//	int idx = cd.name.IndexOf( '(' );
		//	if ( idx > 0 )
		//	{
		//		string nonelite = cd.name.Substring( 0, idx ).Trim();
		//		cardInstruction = DataStore.activationInstructions.Where( x => x.instName == nonelite ).FirstOr( null );
		//		Debug.Log( "TRYING REGULAR INSTRUCTION" );
		//		if ( cardInstruction == null )
		//		{
		//			Debug.Log( "CAN'T FIND INSTRUCTION FOR: " + cd.id + "/" + nonelite );
		//			return;
		//		}
		//	}
		//}

		gameObject.SetActive( true );
		fader.color = new Color( 0, 0, 0, 0 );
		fader.DOFade( .95f, 1 );
		cg.DOFade( 1, .5f );
		transform.GetChild( 1 ).localScale = new Vector3( .85f, .85f, .85f );
		transform.GetChild( 1 ).DOScale( 1, .5f ).SetEase( Ease.OutExpo );

		SetThumbnail( cd );

		enemyName.text = cd.name.ToLower();

		if ( DataStore.gameType == GameType.Saga )
		{
			//check for name override
			var ovrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( cd.id );
			if ( ovrd != null )
				enemyName.text = ovrd.nameOverride.ToLower();

			//check for modififier override
			if ( ovrd != null && ovrd.showMod && !string.IsNullOrEmpty( ovrd.modification.Trim() ) )
			{
				modifierBox.SetActive( true );
				modText.text = ReplaceGlyphs( ovrd.modification );
			}
			else
				modifierBox.SetActive( false );
		}

		if ( !string.IsNullOrEmpty( cd.ignored ) )
			ignoreText.text = $"<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">F</font></color>" + cd.ignored;
		else
			ignoreText.text = "";

		if ( !cardDescriptor.hasActivated )
		{
			//if multiple card instructions, pick 1
			int[] rnd = GlowEngine.GenerateRandomNumbers( cardInstruction.content.Count );
			InstructionOption io = cardInstruction.content[rnd[0]];
			List<string> instructions = io.instruction;
			//check for instruction/repositioning override
			if ( DataStore.gameType == GameType.Saga )
			{
				instructions = GetModifiedInstructions( cd.id, instructions );
				instructions = GetModifiedRepositioning( cd.id, instructions );
			}

			DeploymentCard potentialRebel;
			if ( DataStore.gameType == GameType.Classic )
				potentialRebel = FindRebel();
			else
				potentialRebel = FindRebelSaga();
			if ( potentialRebel != null )
				rebel1 = potentialRebel.name;
			else
				rebel1 = DataStore.uiLanguage.uiMainApp.noneUC;

			ParseInstructions( instructions );
			ParseBonus( cd.id, difficulty );

			//save this card's activation state
			cardDescriptor.hasActivated = true;
			cardDescriptor.rebelName = rebel1;
			cardDescriptor.instructionOption = io;
			cardDescriptor.bonusName = bonusNameText.text;
			cardDescriptor.bonusText = bonusText.text;
		}
		else
		{
			DeploymentCard potentialRebel;
			//get new target
			if ( DataStore.gameType == GameType.Classic )
				potentialRebel = FindRebel();
			else
				potentialRebel = FindRebelSaga();

			//re-use target
			if ( cardDescriptor.rebelName != null && DataStore.gameType == GameType.Classic )
				rebel1 = cardDescriptor.rebelName;
			else if ( potentialRebel != null )
				rebel1 = potentialRebel.name;
			else
				rebel1 = DataStore.uiLanguage.uiMainApp.noneUC;

			//re-use instructions
			if ( cardDescriptor.instructionOption != null && DataStore.gameType == GameType.Classic )
			{
				List<string> instructions = cardDescriptor.instructionOption.instruction;
				//check for instruction override
				if ( DataStore.gameType == GameType.Saga )
				{
					instructions = GetModifiedInstructions( cd.id, instructions );
					instructions = GetModifiedRepositioning( cd.id, instructions );
				}
				ParseInstructions( instructions );
			}
			else//get new instructions for this activation
			{
				InstructionOption io = cardInstruction.content[GlowEngine.GenerateRandomNumbers( cardInstruction.content.Count )[0]];
				List<string> instructions = io.instruction;
				//check for instruction override
				if ( DataStore.gameType == GameType.Saga )
				{
					instructions = GetModifiedInstructions( cd.id, instructions );
					instructions = GetModifiedRepositioning( cd.id, instructions );
				}
				ParseInstructions( instructions );
				cardDescriptor.instructionOption = io;
			}

			if ( cardDescriptor.bonusName != null
				&& cardDescriptor.bonusText != null
				&& DataStore.gameType == GameType.Classic )//re-use activation bonus
			{
				bonusNameText.text = cardDescriptor.bonusName;
				bonusText.text = cardDescriptor.bonusText;
			}
			else//get a new bonus for this activation
			{
				ParseBonus( cd.id, difficulty );
				cardDescriptor.bonusName = bonusNameText.text;
				cardDescriptor.bonusText = bonusText.text;
			}
		}
	}

	void SetThumbnail( DeploymentCard cd )
	{
		//set thumbnail for villain
		if ( DataStore.villainCards.Any( x => x.id == cd.id ) )
			thumbnail.sprite = Resources.Load<Sprite>( $"Cards/Villains/{cd.id.Replace( "DG", "M" )}" );
		else//regular enemy
		{
			thumbnail.sprite = Resources.Load<Sprite>( $"Cards/Enemies/{cd.expansion}/{cd.id.Replace( "DG", "M" )}" );
			thumbnail.GetComponent<Outline>().effectColor = new Color( 0, 0.6440244f, 1, 1 );
		}
		thumbnail.DOFade( 1, .25f );
	}

	void ParseBonus( string id, Difficulty difficulty )
	{
		bonusNameText.text = "";
		bonusText.text = "";
		BonusEffect be = DataStore.bonusEffects.Where( x => x.bonusID == id ).FirstOr( null );
		if ( be == null || be.effects.Count == 0 )
			return;

		//first choose a random bonus
		int[] rnd = GlowEngine.GenerateRandomNumbers( be.effects.Count );
		string e = be.effects[rnd[0]];
		//get the bonus name
		int idx = e.IndexOf( ':' );
		bonusNameText.text = e.Substring( 0, idx );
		bonusText.text = ReplaceGlyphs( e.Substring( idx + 1 ) ).Trim();

		//At each activation, there’s a 25% chance that no bonus effect will be applied
		if ( difficulty == Difficulty.Easy )
		{
			if ( GlowEngine.RandomBool( 25 ) )
			{
				Debug.Log( "EASY MODE: applied 25% chance bonus skipped" );
				bonusNameText.text = "";
				bonusText.text = "";
			}
		}
	}

	void ParseInstructions( List<string> instruction )
	{
		Transform content = transform.Find( "Panel/content" );

		for ( int i = 0; i < instruction.Count; i++ )
		{
			string item = instruction[i];

			GameObject go = new GameObject( "content item" );
			go.layer = 5;
			go.transform.SetParent( content );
			go.transform.localScale = Vector3.one;
			go.transform.localEulerAngles = Vector3.zero;

			TextMeshProUGUI nt = go.AddComponent<TextMeshProUGUI>();
			nt.color = Color.white;
			nt.fontSize = 25;

			//replace glyphs
			item = ReplaceGlyphs( item );

			//add bullets
			if ( item.Contains( "{-}" ) )
			{
				nt.color = new Color( 0, 0.6440244f, 1, 1 );
				//nt.margin = new Vector4( 25, 0, 0, 0 );
				//item = item.Replace( "{-}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">U</font></color> " );
				item = item.Replace( "{-}", " \u25A0 " );
			}
			//orange highlight
			if ( item.Contains( "{O}" ) )
			{
				item = item.Replace( "{O}", "" );
				nt.color = new Color( 1, 0.5586207f, 0, 1 );
			}

			nt.text = item;
			var rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2( 1100, 100 );
		}
	}

	string ReplaceGlyphs( string item )
	{
		item = item.Replace( "{H}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">H</font></color>" );
		item = item.Replace( "{C}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">C</font></color>" );
		item = item.Replace( "{J}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">J</font></color>" );
		item = item.Replace( "{K}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">K</font></color>" );
		item = item.Replace( "{A}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">A</font></color>" );
		item = item.Replace( "{Q}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">Q</font></color>" );
		item = item.Replace( "{g}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">g</font></color>" );
		item = item.Replace( "{h}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">h</font></color>" );
		item = item.Replace( "{E}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">E</font></color>" );
		item = item.Replace( "{G}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">G</font></color>" );
		item = item.Replace( "{f}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">f</font></color>" );
		item = item.Replace( "{b}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">b</font></color>" );
		item = item.Replace( "{B}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">B</font></color>" );
		item = item.Replace( "{I}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">I</font></color>" );
		item = item.Replace( "{P}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">P</font></color>" );
		item = item.Replace( "{F}", "<color=\"red\"><font=\"ImperialAssaultSymbols SDF\">F</font></color>" );

		if ( item.Contains( "{R1}" ) )
		{
			item = item.Replace( "{R1}", "<color=#00A4FF>" + rebel1 + "</color>" );
		}

		return item;
	}

	DeploymentCard FindRebelSaga()
	{
		DeploymentCard defaultRebel = null;
		//try to get preferred targets first (defaults)
		GroupTraits[] groupTraits = cardDescriptor.preferredTargets;

		//check for target trait override
		var traitOvrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( cardDescriptor.id )?.groupTraits;
		if ( traitOvrd != null )
		{
			Debug.Log( $"FindRebel()::Preferred Traits OVERRIDE" );
			groupTraits = traitOvrd;
		}

		//check for target trait override in ChangeTarget, which gets the last say
		//all
		var ovrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride()?.changeTarget;
		if ( ovrd != null && ovrd.targetType == PriorityTargetType.Trait )
		{
			Debug.Log( $"FindRebel()::MODIFYING ALL::{ovrd.targetType}" );
			if ( !ovrd.groupPriorityTraits.useDefaultPriority )
				groupTraits = ovrd.groupPriorityTraits.GetTraitArray();
		}
		//specific
		ovrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( cardDescriptor.id )?.changeTarget;
		if ( ovrd != null && ovrd.targetType == PriorityTargetType.Trait )
		{
			Debug.Log( $"FindRebel()::MODIFYING SPECIFIC::{ovrd.targetType}" );
			if ( !ovrd.groupPriorityTraits.useDefaultPriority )
				groupTraits = ovrd.groupPriorityTraits.GetTraitArray();
		}

		var hlist = DataStore.deployedHeroes.GetHealthy().WithTraits( groupTraits );
		var ulist = DataStore.deployedHeroes.GetUnhealthy().WithTraits( groupTraits );
		if ( hlist is null )
			hlist = DataStore.deployedHeroes.GetHealthy();
		else
			Debug.Log( "PREFFERED::" + string.Join( ", ", groupTraits ) );
		if ( ulist is null )
			ulist = DataStore.deployedHeroes.GetUnhealthy();

		if ( hlist != null )
		{
			//Debug.Log( "healthy HEROES: " + hlist.Count );
			int[] rnd = GlowEngine.GenerateRandomNumbers( hlist.Count() );
			defaultRebel = hlist[rnd[0]];
		}
		else if ( ulist != null )
		{
			//Debug.Log( "UNhealthy HEROES: " + ulist.Count );
			int[] rnd = GlowEngine.GenerateRandomNumbers( ulist.Count() );
			defaultRebel = ulist[rnd[0]];
		}

		//check for target override
		//if a targeted ally/hero doesn't exist in the game, or it's withdrawn, just use default
		//all
		ovrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride()?.changeTarget;
		if ( ovrd != null )
		{
			if ( ovrd.targetType == PriorityTargetType.Rebel )
			{
				//default behavior - any rebel already determined above
			}
			if ( ovrd.targetType == PriorityTargetType.Ally )
			{
				defaultRebel = DataStore.deployedHeroes.Where( x => x.id == ovrd.specificAlly && x.heroState.heroHealth != HeroHealth.Defeated ).FirstOr( null ) ?? defaultRebel;
			}
			else if ( ovrd.targetType == PriorityTargetType.Hero )
			{
				defaultRebel = DataStore.deployedHeroes.Where( x => x.id == ovrd.specificHero && x.heroState.heroHealth != HeroHealth.Defeated ).FirstOr( null ) ?? defaultRebel;
			}
			Debug.Log( $"FindRebel()::MODIFYING ALL::{ovrd.targetType}, target = {defaultRebel.name}" );
		}

		//specific
		ovrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( cardDescriptor.id )?.changeTarget;
		if ( ovrd != null )
		{
			if ( ovrd.targetType == PriorityTargetType.Rebel )
			{
				//default behavior - any rebel already determined above
			}
			if ( ovrd.targetType == PriorityTargetType.Ally )
			{
				defaultRebel = DataStore.deployedHeroes.Where( x => x.id == ovrd.specificAlly && x.heroState.heroHealth != HeroHealth.Defeated ).FirstOr( null ) ?? defaultRebel;
			}
			else if ( ovrd.targetType == PriorityTargetType.Hero )
			{
				defaultRebel = DataStore.deployedHeroes.Where( x => x.id == ovrd.specificHero && x.heroState.heroHealth != HeroHealth.Defeated ).FirstOr( null ) ?? defaultRebel;
			}
			Debug.Log( $"FindRebel()::MODIFYING SPECIFIC::{ovrd.targetType}, target = {defaultRebel.name}" );
		}

		return defaultRebel;
	}

	DeploymentCard FindRebel()
	{
		var hlist = DataStore.deployedHeroes.GetHealthy();
		var ulist = DataStore.deployedHeroes.GetUnhealthy();
		DeploymentCard defaultRebel = null;


		if ( hlist != null )
		{
			//Debug.Log( "healthy HEROES: " + hlist.Count );
			int[] rnd = GlowEngine.GenerateRandomNumbers( hlist.Count() );
			defaultRebel = hlist[rnd[0]];
		}
		else if ( ulist != null )
		{
			//Debug.Log( "UNhealthy HEROES: " + ulist.Count );
			int[] rnd = GlowEngine.GenerateRandomNumbers( ulist.Count() );
			defaultRebel = ulist[rnd[0]];
		}

		return defaultRebel;
	}

	public void OnViewCard()
	{
		spaceListen = false;
		EventSystem.current.SetSelectedGameObject( null );

		CardViewPopup cardViewPopup = GlowEngine.FindUnityObject<CardViewPopup>();
		cardViewPopup.Show( cardDescriptor, OnReturn );
	}

	public void OnReturn( bool value = true )
	{
		spaceListen = value;
	}

	public void OnClose()
	{
		FindObjectOfType<Sound>().PlaySound( FX.Click );
		fader.DOFade( 0, .5f ).OnComplete( () =>
		{
			Transform content = transform.Find( "Panel/content" );
			foreach ( Transform tf in content )
				Destroy( tf.gameObject );
			gameObject.SetActive( false );
		} );
		cg.DOFade( 0, .2f );
		transform.GetChild( 1 ).DOScale( .85f, .5f ).SetEase( Ease.OutExpo );
		callback?.Invoke();
	}

	private void Update()
	{
		if ( spaceListen && Input.GetKeyDown( KeyCode.Space ) )
			OnClose();
	}

	public void OnRollAttackDice()
	{
		if ( cardDescriptor.attacks == null )
			return;

		FindObjectOfType<Sound>().PlaySound( FX.Click );
		spaceListen = false;
		EventSystem.current.SetSelectedGameObject( null );
		diceRoller.Show( cardDescriptor, true, OnReturn );
	}

	public void OnRollDefenseDice()
	{
		if ( cardDescriptor.defense == null )
			return;

		FindObjectOfType<Sound>().PlaySound( FX.Click );
		spaceListen = false;
		EventSystem.current.SetSelectedGameObject( null );
		diceRoller.Show( cardDescriptor, false, OnReturn );
	}

	public List<string> GetModifiedInstructions( string ID, List<string> linesOut )
	{
		//all
		var ci = DataStore.sagaSessionData.gameVars.GetDeploymentOverride()?.changeInstructions;
		if ( ci != null )
		{
			List<string> lines = ci.theText.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			if ( ci.instructionType == CustomInstructionType.Replace )
				linesOut = lines;
			else if ( ci.instructionType == CustomInstructionType.Top )
				linesOut = lines.Concat( linesOut ).ToList();
			else if ( ci.instructionType == CustomInstructionType.Bottom )
				linesOut = linesOut.Concat( lines ).ToList();
			Debug.Log( $"GetModifiedInstructions()::ALL::MODIFYING WITH {lines.Count} LINES::{ci.instructionType}" );
		}

		//specific
		var dgOvrd = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( ID )?.changeInstructions;
		if ( dgOvrd != null )
		{
			List<string> lines = dgOvrd.theText.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			if ( dgOvrd.instructionType == CustomInstructionType.Replace )
				linesOut = lines;
			else if ( dgOvrd.instructionType == CustomInstructionType.Top )
				linesOut = lines.Concat( linesOut ).ToList();
			else if ( dgOvrd.instructionType == CustomInstructionType.Bottom )
				linesOut = linesOut.Concat( lines ).ToList();
			Debug.Log( $"GetModifiedInstructions()::MODIFYING WITH {lines.Count} LINES::{dgOvrd.instructionType}" );
		}

		return linesOut;
	}

	public List<string> GetModifiedRepositioning( string ID, List<string> linesOut )
	{
		//all
		string repo = DataStore.sagaSessionData.gameVars.GetDeploymentOverride()?.repositionInstructions;
		if ( !string.IsNullOrEmpty( repo ) )
		{
			repo = "<color=orange>{-} " + repo + "</color>";
			List<string> lines = repo.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			lines.Insert( 0, "\n" );
			//place at bottom of instructions
			linesOut = linesOut.Concat( lines ).ToList();
			Debug.Log( $"GetModifiedRepositioning()::ALL::MODIFYING WITH {lines.Count} LINES::{repo}" );
		}

		//specific
		repo = DataStore.sagaSessionData.gameVars.GetDeploymentOverride( ID )?.repositionInstructions;
		if ( !string.IsNullOrEmpty( repo ) )
		{
			repo = "<color=orange>{-} " + repo + "</color>";
			List<string> lines = repo.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			lines.Insert( 0, "\n" );
			//place at bottom of instructions
			linesOut = linesOut.Concat( lines ).ToList();
			Debug.Log( $"GetModifiedRepositioning()::ALL::MODIFYING WITH {lines.Count} LINES::{repo}" );
		}

		return linesOut;
	}
}