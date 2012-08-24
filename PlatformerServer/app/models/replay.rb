class Replay < ActiveRecord::Base
  attr_accessible :data, :level_id, :player, :score
  belongs_to :level

end
