function OnRegister(move)

	move.SetName("vine wrap")
	move.SetDescription("Tightly wraps vines around the opponent, causing them to take damage every turn.")

	move.SetPP(10)

	move.Requires.TypeMatch("producer").MinimumLevel(20).DescriptionMatch("vine")

end

function OnMove(args) 
	
	args.Target.SetStatus("vine-wrapped")

	args.SetText("wrapping the opponent in vines")

end