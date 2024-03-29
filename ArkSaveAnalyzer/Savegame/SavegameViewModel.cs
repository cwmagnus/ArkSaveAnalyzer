﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArkSaveAnalyzer.Infrastructure;
using ArkSaveAnalyzer.Infrastructure.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using SavegameToolkit;
using SavegameToolkitAdditions;

namespace ArkSaveAnalyzer.Savegame {

    public class SavegameViewModel : ViewModelBase {
        private string currentMapName;
        private string fileName;

        public RelayCommand<string> ContentCommand { get; }
        public RelayCommand ShowDataCommand { get; }
        public RelayCommand OpenFileCommand { get; }

        public ObservableCollection<GameObject> Objects { get; } = new ObservableCollection<GameObject>();

        #region SelectedObject

        private GameObject selectedObject;

        public GameObject SelectedObject {
            get => selectedObject;
            set => Set(ref selectedObject, value);
        }

        #endregion

        #region FilterText

        private string filterText;

        public string FilterText {
            get => filterText;
            set {
                if (Set(ref filterText, value)) {
                    loadContent(currentMapName);
                }
            }
        }

        #endregion

        #region GotoId

        private string gotoId;

        public string GotoId {
            get => gotoId;
            set {
                if (Set(ref gotoId, value) && int.TryParse(value, out int id)) {
                    Messenger.Default.Send(new GotoIdMessage(id));
                }
            }
        }

        #endregion

        #region UiEnabled

        private bool uiEnabled = true;

        public bool UiEnabled {
            get => uiEnabled;
            set {
                if (Set(ref uiEnabled, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        public SavegameViewModel() {
            ContentCommand = new RelayCommand<string>(mapName => loadContent(mapName, false), s => UiEnabled);
            ShowDataCommand = new RelayCommand(showData, () => UiEnabled);
            OpenFileCommand = new RelayCommand(openFile, () => UiEnabled);

            Messenger.Default.Register<InvalidateMapDataMessage>(this, message => {
                Application.Current.Dispatcher.Invoke(() => {
                    if (currentMapName == message.MapName) {
                        //Objects.Clear();
                    }
                });
            });
        }

        private async void loadContent(string mapName, bool getCached = true) {
            UiEnabled = false;

            try {
                GameObjectContainer objects = await SavegameService.GetGameObjects(mapName, getCached);

                IEnumerable<GameObject> filteredObjects = objects;
                if (!string.IsNullOrWhiteSpace(filterText)) {
                    filteredObjects = objects.Where(o =>
                            o.ClassString.ToLowerInvariant().Contains(filterText.ToLowerInvariant()) ||
                            o.Names.Any(s => s.ToString().ToLowerInvariant().Contains(filterText.ToLowerInvariant())));
                }

                currentMapName = mapName;
                Objects.Clear();
                foreach (GameObject obj in filteredObjects) {
                    Objects.Add(obj);
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }

            UiEnabled = true;
        }

        private void showData() {
            if (SelectedObject != null) {
                Messenger.Default.Send(new ShowGameObjectMessage(SelectedObject));
            }
        }

        private void openFile() {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                    FileName = fileName,
                    CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
                loadContent(fileName, false);
            }
        }

        private async void specialCommand() {
            UiEnabled = false;

            try {
                GameObjectContainer objects = await SavegameService.GetGameObjects(currentMapName);

                IEnumerable<GameObject> filteredObjects = objects
                        .Where(o => !o.IsCreature() && (o.HasAnyProperty("OwningPlayerID") || o.HasAnyProperty("OwnerName")));
                //.Where(o => o.ClassString == "PrimalItem_WeaponMetalHatchet_C" && !o.GetPropertyValue<bool>("bIsEngram"));

                Objects.Clear();
                foreach (GameObject obj in filteredObjects) {
                    Objects.Add(obj);
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }

            UiEnabled = true;
        }
    }

}
