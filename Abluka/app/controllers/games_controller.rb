require 'securerandom'
require 'matrix_helper'

class GamesController < ApplicationController
  def index
  end

  def show
    @player = params[:id]
    @game = Game.where('player_one = ? OR player_two = ?', @player, @player).first
    @current_player = @game.current_player(@player)
  end

  def create
    @game = Game.new(params[:game]) do |g|
      g.board = initial_board
      g.player_one = new_player
      g.player_two = new_player
      g.current_turn = 1
    end
    
    @game.save
    redirect_to :action => :show, :id => @game.player_one
  end
  
  def update
    @player = params[:id]
    @board = params[:board]
    @game = Game.where('player_one = ? OR player_two = ?', @player, @player).first
    # Validate current player (who's turn is it)
    # Validate board change (is move valid)
    # Clear invite_sent
    redirect_to :action => :show, :id => @player
  end
  
  def invite
    @player = params[:id]
    @game = Game.where('player_one = ? OR player_two = ?', @player, @player).first
    OpponentMailer.challenge(params[:opponent], @game.other_player(@player)).deliver
    @game.invite_sent = true
    @game.save
    redirect_to :action => :show, :id => @player, :notice => 'Opponent invited!'
  end
  
  def initial_board
    Matrix.rows([[1, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 2]])    
  end
  
  def new_player
    SecureRandom.urlsafe_base64(16)
  end
end
