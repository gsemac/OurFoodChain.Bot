function register(move)
  move.name = "intimidating snarl"
  move.description = "Makes a sound that intimidates the opponent, lowering their attack power. Only works on opponents with ears."
  
  move.pp = 10
  move.requires.match = "\\b(vocal|cry|shout|scream|bellow|hiss|bark|cries|squeal)"
  move.requires.maxLevel = 19
end

function callback(args)
  if args.TargetHasDescription("\\b(ear|hear|listen|echolocate|echolocation)") then
    args.target.stats.atk = args.target.stats.atk * 0.5
  else
    args.text = "but it failed"
  end
end
