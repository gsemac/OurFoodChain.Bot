function OnRegister(move)

	move.SetName("skill steal")
	move.SetDescription("Swaps a random stat with the opponent.")

	move.SetPP(15)
	
	move.Requires.TypeMatch("parasite").MinimumLevel(20)

end

function OnMove(args) 	
	
	r = rand(0, 3)

	if(r == 0) then
		swap(args.User.Stats.Atk, args.Target.Stats.Atk)
	elseif(r == 1) then
		swap(args.User.Stats.Def, args.Target.Stats.Def)
	elseif(r == 2) then
		swap(args.User.Stats.Spd, args.Target.Stats.Spd)
	end

	args.SetText("swapping a stat with the opponent")

end