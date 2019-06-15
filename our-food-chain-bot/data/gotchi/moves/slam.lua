function register(move)

	move.name = "slam"
	move.description = "Strikes the opponent with a heavy appendage. This move deals a lot of damage, but always strikes last."
	
	move.pp = 5
	move.type = type.Offensive
	move.multiplier = 2.0
	move.priority = -1
	
	move.requires.minLevel = 30
	move.requires.match = "heavy tail|heavy limb|heavy appendage|thick tail|thick appendage"

end

function callback(args)
	args.DoDamage()
end