function OnRegister(move)

	move.SetName("seed drop")
	move.SetDescription("Drops 1-5 seeds onto the opponent, dealing minor damage repeatedly.")
	move.SetType("producer")

	move.SetPower(15)
	move.SetPP(25)
	move.SetAccuracy(0.9)

	move.Requires.TypeMatch("producer").MinimumLevel(20).DescriptionMatch("seed")

end

function OnInit(args) 
	args.SetTimes(rand(1, 6))
end