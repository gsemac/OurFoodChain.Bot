function OnRegister(move)

	move.SetName("Night Strike")
	move.SetDescription("Attacks in the night while the opponent is sleeping, completely ignoring defense. Has a 50% chance to fail if the target wakes up.")

	move.SetPP(1)

	move.Requires.MinimumLevel(25)
	move.Requires.DescriptionMatch("\\b(?:nocturnal)\\b")

end

function OnMove(args) 
	
	if(chance(2)) then
		args.SetText("but the opponent woke up")
	else 

		old_defense = args.Target.Stats.Def
		
		args.Target.Stats.Def = 0
		
		args.DealDamage()
		
		args.Target.Stats.Def = old_defense;

	end

end