require 'test_helper'

class ReplaysControllerTest < ActionController::TestCase
  setup do
    @replay = replays(:one)
  end

  test "should get index" do
    get :index
    assert_response :success
    assert_not_nil assigns(:replays)
  end

  test "should get new" do
    get :new
    assert_response :success
  end

  test "should create replay" do
    assert_difference('Replay.count') do
      post :create, replay: { data: @replay.data, level_id: @replay.level_id, player: @replay.player, score: @replay.score }
    end

    assert_redirected_to replay_path(assigns(:replay))
  end

  test "should show replay" do
    get :show, id: @replay
    assert_response :success
  end

  test "should get edit" do
    get :edit, id: @replay
    assert_response :success
  end

  test "should update replay" do
    put :update, id: @replay, replay: { data: @replay.data, level_id: @replay.level_id, player: @replay.player, score: @replay.score }
    assert_redirected_to replay_path(assigns(:replay))
  end

  test "should destroy replay" do
    assert_difference('Replay.count', -1) do
      delete :destroy, id: @replay
    end

    assert_redirected_to replays_path
  end
end
