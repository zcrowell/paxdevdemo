require 'matrix'
require 'json'

class Game < ActiveRecord::Base
  attr_accessible :board
  attr_accessible :player_one
  attr_accessible :player_two
  attr_accessible :current_turn
  attr_accessible :invite_sent
  attr_accessible :winner

  def board=(board)
    write_attribute(:board, board.to_a.to_json)
  end

  def board
    Matrix.rows(JSON.parse(read_attribute(:board)))
  end

  def current_player(player)
    return 1 if player == player_one
    return 2 if player == player_two
    
    nil
  end

  def other_player(player)
    return player_two if player == player_one
    return player_one if player == player_two
    
    nil
  end
end
