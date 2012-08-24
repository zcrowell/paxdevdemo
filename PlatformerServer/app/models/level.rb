class Level < ActiveRecord::Base
  attr_accessible :content, :name
  has_many :replays, :dependent => :delete_all
end
