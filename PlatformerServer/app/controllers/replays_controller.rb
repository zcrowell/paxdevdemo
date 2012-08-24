class ReplaysController < ApplicationController
  http_basic_authenticate_with :name => "julien", :password => "secret", :except => [:index,:show]


  # GET /replays
  # GET /replays.json
  def index
    if(params[:level_id] == nil)
      @replays = Replay.all(:order => 'level_id, score DESC')
    else
      @replays = Replay.where(:level_id => params[:level_id]).order(:score)
    end

    respond_to do |format|
      format.html # index.html.erb
      format.json { render :json => @replays }
    end
  end

  # GET /replays/1
  # GET /replays/1.json
  def show
    @replay = Replay.find(params[:id])

    send_data @replay.data, :filename => @replay.level_id.to_s + ".replay"
  end

  # GET /replays/best/1
  def best
    begin
      @replay = Replay.where(:level_id => params[:level_id]).order(:score).first!
      send_data @replay.data, :filename => @replay.level_id.to_s + ".replay"
    rescue ActiveRecord::RecordNotFound
      render(:file => "#{Rails.root}/public/404.html", :layout => false, :status => 404)
    end
  end

  # GET /replays/new
  # GET /replays/new.json
  def new
    @replay = Replay.new

    respond_to do |format|
      format.html # new.html.erb
      format.json { render :json => @replay }
    end
  end

  # GET /replays/1/edit
  def edit
    @replay = Replay.find(params[:id])
  end

  # POST /replays
  # POST /replays.json
  def create

    @replay = Replay.new()
    @replay.level_id = params[:replay][:level_id]
    @replay.score = params[:replay][:score]
    @replay.player = params[:replay][:player]
    @replay.data = params[:replay][:data].read

    respond_to do |format|
      if @replay.save
        format.html { redirect_to replays_path, :notice => 'Replay was successfully created.' }
      else
        format.html { render :action => "new" }
      end
    end
  end

  # PUT /replays/1
  # PUT /replays/1.json
  def update
    @replay = Replay.find(params[:id])
    @replay.level_id = params[:replay][:level_id]
    @replay.score = params[:replay][:score]
    @replay.player = params[:replay][:player]
    @replay.data = params[:replay][:data].read

    respond_to do |format|

        format.html { redirect_to replays_path, :notice => 'Replay was successfully updated.' }
        format.json { head :no_content }

    end
  end

  # DELETE /replays/1
  # DELETE /replays/1.json
  def destroy
    @replay = Replay.find(params[:id])
    @replay.destroy

    respond_to do |format|
      format.html { redirect_to replays_url }
      format.json { head :no_content }
    end
  end
end
