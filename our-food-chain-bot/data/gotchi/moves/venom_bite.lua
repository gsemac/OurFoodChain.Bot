function OnRegister(move)

	move.SetName("Venom Bite")
	move.SetDescription("Bites the opponent, injecting venom into the wound. Has a small chance of poisoning the target.")
	move.SetType("predator")

	move.SetPP(15)

	move.Requires.TypeMatch("predator").DescriptionMatch("poison|venom")

end

function OnMove(args) 

	if(chance(10)) then
		args.Target.SetStatus("poisoned")
	end;

	args.DealDamage()

end