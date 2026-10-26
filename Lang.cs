using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MonitorHub
{
    // Traduções. A chave é o texto em português; valores: [inglês, espanhol, francês, alemão]
    public static class L
    {
        public static readonly string[] Codes = { "pt", "en", "es", "fr", "de" };
        public static readonly string[] Names = { "Português", "English", "Español", "Français", "Deutsch" };
        public static int Cur;
        static string FileP { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorHub", "lang.txt"); } }

        public static void Load()
        {
            string c = null;
            try { c = File.ReadAllText(FileP).Trim(); } catch { }
            if (string.IsNullOrEmpty(c)) c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            Cur = Math.Max(0, Array.IndexOf(Codes, c));
            if (Array.IndexOf(Codes, c) < 0) Cur = 1;
        }
        public static void Set(int i) { try { Directory.CreateDirectory(Path.GetDirectoryName(FileP)); File.WriteAllText(FileP, Codes[i]); } catch { } Cur = i; }

        public static string T(string pt)
        {
            string[] v;
            if (Cur == 0 || !D.TryGetValue(pt, out v)) return pt;
            return v[Cur - 1];
        }
        public static string F(string pt, params object[] a) { return string.Format(T(pt), a); }

        static readonly Dictionary<string, string[]> D = new Dictionary<string, string[]>
        {
            // ---- janela principal ----
            { "Pelo menos um monitor precisa estar ativo.", new[] { "At least one monitor must be active.", "Al menos un monitor debe estar activo.", "Au moins un écran doit être actif.", "Mindestens ein Monitor muss aktiv sein." } },
            { "Falha ao configurar {0} (código {1}).", new[] { "Failed to configure {0} (code {1}).", "Error al configurar {0} (código {1}).", "Échec de la configuration de {0} (code {1}).", "Fehler beim Einrichten von {0} (Code {1})." } },
            { "Definir como principal", new[] { "Set as main", "Establecer como principal", "Définir comme principal", "Als Hauptmonitor festlegen" } },
            { "Definir {0} como principal", new[] { "Set {0} as main", "Establecer {0} como principal", "Définir {0} comme principal", "{0} als Hauptmonitor festlegen" } },
            { "{0} é o principal", new[] { "{0} is the main display", "{0} es el principal", "{0} est l'écran principal", "{0} ist der Hauptmonitor" } },
            { "Desabilitar", new[] { "Disable", "Desactivar", "Désactiver", "Deaktivieren" } },
            { "O monitor principal não pode ser desativado.", new[] { "The main monitor can't be disabled.", "El monitor principal no se puede desactivar.", "L'écran principal ne peut pas être désactivé.", "Der Hauptmonitor kann nicht deaktiviert werden." } },
            { "{0} será desativado. Clique em Aplicar.", new[] { "{0} will be disabled. Click Apply.", "{0} se desactivará. Haz clic en Aplicar.", "{0} sera désactivé. Cliquez sur Appliquer.", "{0} wird deaktiviert. Klicke auf Übernehmen." } },
            { "{0} será ativado. Clique em Aplicar.", new[] { "{0} will be enabled. Click Apply.", "{0} se activará. Haz clic en Aplicar.", "{0} sera activé. Cliquez sur Appliquer.", "{0} wird aktiviert. Klicke auf Übernehmen." } },
            { "{0} reposicionado. Clique em Aplicar.", new[] { "{0} moved. Click Apply.", "{0} movido. Haz clic en Aplicar.", "{0} déplacé. Cliquez sur Appliquer.", "{0} verschoben. Klicke auf Übernehmen." } },
            { "{0} ajustado. Clique em Aplicar.", new[] { "{0} adjusted. Click Apply.", "{0} ajustado. Haz clic en Aplicar.", "{0} ajusté. Cliquez sur Appliquer.", "{0} angepasst. Klicke auf Übernehmen." } },
            { "RESOLUÇÃO", new[] { "RESOLUTION", "RESOLUCIÓN", "RÉSOLUTION", "AUFLÖSUNG" } },
            { "TAXA DE ATUALIZAÇÃO", new[] { "REFRESH RATE", "FRECUENCIA", "FRÉQUENCE", "BILDWIEDERHOLRATE" } },
            { "ESCALA DO WINDOWS", new[] { "WINDOWS SCALING", "ESCALA DE WINDOWS", "MISE À L'ÉCHELLE", "WINDOWS-SKALIERUNG" } },
            { "Calibração de cores", new[] { "Color calibration", "Calibración de color", "Calibrage des couleurs", "Farbkalibrierung" } },
            { "Calibração CRT", new[] { "CRT calibration", "Calibración CRT", "Calibrage CRT", "CRT-Kalibrierung" } },
            { "Recarregar", new[] { "Reload", "Recargar", "Recharger", "Neu laden" } },
            { "Aplicar", new[] { "Apply", "Aplicar", "Appliquer", "Übernehmen" } },
            { "CONFIGURAÇÕES", new[] { "PROFILES", "PERFILES", "PROFILS", "PROFILE" } },
            { "Salvar configuração", new[] { "Save profile", "Guardar perfil", "Enregistrer le profil", "Profil speichern" } },
            { "Nome da configuração", new[] { "Profile name", "Nombre del perfil", "Nom du profil", "Profilname" } },
            { "Configuração \"{0}\" salva.", new[] { "Profile \"{0}\" saved.", "Perfil \"{0}\" guardado.", "Profil « {0} » enregistré.", "Profil „{0}“ gespeichert." } },
            { "SALVAS", new[] { "SAVED", "GUARDADOS", "ENREGISTRÉS", "GESPEICHERT" } },
            { "Nenhuma ainda.", new[] { "None yet.", "Ninguno todavía.", "Aucun pour l'instant.", "Noch keine." } },
            { "Aplicando \"{0}\"...", new[] { "Applying \"{0}\"...", "Aplicando \"{0}\"...", "Application de « {0} »...", "„{0}“ wird angewendet..." } },
            { "Salvar", new[] { "Save", "Guardar", "Enregistrer", "Speichern" } },
            { "Cancelar", new[] { "Cancel", "Cancelar", "Annuler", "Abbrechen" } },
            { "{0} monitor(es) detectado(s).", new[] { "{0} monitor(s) detected.", "{0} monitor(es) detectado(s).", "{0} écran(s) détecté(s).", "{0} Monitor(e) erkannt." } },
            { "Ativar {0}", new[] { "Enable {0}", "Activar {0}", "Activer {0}", "{0} aktivieren" } },
            { "PRINCIPAL", new[] { "MAIN", "PRINCIPAL", "PRINCIPAL", "HAUPT" } },
            { "arraste ou use as setas (Shift = 10px, Ctrl = 100px)", new[] { "drag or use the arrow keys (Shift = 10px, Ctrl = 100px)", "arrastra o usa las flechas (Shift = 10px, Ctrl = 100px)", "glissez ou utilisez les flèches (Maj = 10px, Ctrl = 100px)", "ziehen oder Pfeiltasten (Umschalt = 10px, Strg = 100px)" } },
            { "Alterações revertidas.", new[] { "Changes reverted.", "Cambios revertidos.", "Modifications annulées.", "Änderungen rückgängig gemacht." } },
            { "Configurações aplicadas.", new[] { "Settings applied.", "Configuración aplicada.", "Paramètres appliqués.", "Einstellungen übernommen." } },
            { "Manter", new[] { "Keep", "Mantener", "Conserver", "Behalten" } },
            { "Reverter", new[] { "Revert", "Revertir", "Annuler", "Zurücksetzen" } },
            { "Manter estas configurações de vídeo?\nRevertendo em {0}s...", new[] { "Keep these display settings?\nReverting in {0}s...", "¿Mantener esta configuración de pantalla?\nRevirtiendo en {0}s...", "Conserver ces paramètres d'affichage ?\nRetour dans {0}s...", "Diese Anzeigeeinstellungen behalten?\nZurücksetzen in {0}s..." } },
            { "O Monitor Hub já está aberto na bandeja do sistema.", new[] { "Monitor Hub is already running in the system tray.", "Monitor Hub ya está abierto en la bandeja del sistema.", "Monitor Hub est déjà ouvert dans la zone de notification.", "Monitor Hub läuft bereits im Infobereich." } },
            { "Calibrar geometria CRT", new[] { "Calibrate CRT geometry", "Calibrar geometría CRT", "Calibrer la géométrie CRT", "CRT-Geometrie kalibrieren" } },
            { "Esse monitor precisa estar ativo.", new[] { "This monitor must be enabled.", "Este monitor debe estar activo.", "Cet écran doit être actif.", "Dieser Monitor muss aktiv sein." } },
            { "Fechar", new[] { "Close", "Cerrar", "Fermer", "Schließen" } },
            { "Iniciar com o Windows", new[] { "Start with Windows", "Iniciar con Windows", "Lancer avec Windows", "Mit Windows starten" } },
            { "Atalho", new[] { "Open", "Abrir", "Ouvrir", "Öffnen" } },
            { "Sair", new[] { "Exit", "Salir", "Quitter", "Beenden" } },

            // ---- calibração (rodapé) ----
            { "clique / → próximo   ·   botão direito / ← anterior   ·   H esconde dicas   ·   Esc sai", new[] { "click / → next   ·   right-click / ← previous   ·   H hides tips   ·   Esc exits", "clic / → siguiente   ·   clic derecho / ← anterior   ·   H oculta consejos   ·   Esc sale", "clic / → suivant   ·   clic droit / ← précédent   ·   H masque l'aide   ·   Échap quitte", "Klick / → weiter   ·   Rechtsklick / ← zurück   ·   H blendet Tipps aus   ·   Esc beendet" } },
            { "↑ ↓ ajustam", new[] { "↑ ↓ adjust", "↑ ↓ ajustan", "↑ ↓ ajustent", "↑ ↓ anpassen" } },

            // ---- calibração de cores ----
            { "Branco 100%", new[] { "White 100%", "Blanco 100%", "Blanc 100%", "Weiß 100%" } },
            { "Compare o tom do branco entre os monitores. Ajuste os ganhos R/G/B no menu do monitor até ficarem iguais (nem azulado, nem amarelado).", new[] { "Compare the white tint between monitors. Adjust R/G/B gains in the monitor menu until they match (neither bluish nor yellowish).", "Compara el tono del blanco entre monitores. Ajusta las ganancias R/G/B en el menú del monitor hasta que coincidan.", "Comparez la teinte du blanc entre les écrans. Ajustez les gains R/V/B dans le menu de l'écran jusqu'à ce qu'ils soient identiques.", "Vergleiche den Weißton der Monitore. Passe die R/G/B-Werte im Monitormenü an, bis sie gleich sind." } },
            { "Cinza 50%", new[] { "Gray 50%", "Gris 50%", "Gris 50%", "Grau 50%" } },
            { "Os dois devem ter o mesmo cinza neutro. Um tom rosado, verde ou azul indica ganho R/G/B desequilibrado.", new[] { "Both should show the same neutral gray. A pink, green or blue tint means unbalanced R/G/B gain.", "Ambos deben mostrar el mismo gris neutro. Un tono rosado, verde o azul indica ganancias R/G/B desequilibradas.", "Les deux doivent afficher le même gris neutre. Une teinte rose, verte ou bleue indique des gains R/V/B déséquilibrés.", "Beide sollten dasselbe neutrale Grau zeigen. Ein rosa, grüner oder blauer Stich bedeutet unausgewogene R/G/B-Werte." } },
            { "Cinza 20%", new[] { "Gray 20%", "Gris 20%", "Gris 20%", "Grau 20%" } },
            { "Cinza escuro neutro. Ajuda a ver diferenças de cor em tons baixos.", new[] { "Neutral dark gray. Helps spot color differences in low tones.", "Gris oscuro neutro. Ayuda a ver diferencias de color en tonos bajos.", "Gris foncé neutre. Aide à voir les écarts de couleur dans les tons sombres.", "Neutrales Dunkelgrau. Zeigt Farbunterschiede in dunklen Tönen." } },
            { "Degraus de cinza", new[] { "Gray steps", "Escalones de gris", "Paliers de gris", "Graustufen" } },
            { "Os 16 degraus devem ser todos distinguíveis e neutros, sem tons coloridos.", new[] { "All 16 steps should be distinguishable and neutral, with no color tint.", "Los 16 escalones deben distinguirse y ser neutros, sin tonos de color.", "Les 16 paliers doivent être distincts et neutres, sans teinte.", "Alle 16 Stufen sollten unterscheidbar und neutral sein, ohne Farbstich." } },
            { "Rampa de cinza", new[] { "Gray ramp", "Rampa de gris", "Dégradé de gris", "Graurampe" } },
            { "Deve ser suave, sem faixas marcadas e sem mudar de cor ao longo da rampa.", new[] { "Should be smooth, without banding or color shifts along the ramp.", "Debe ser suave, sin bandas ni cambios de color.", "Doit être fluide, sans bandes ni changement de teinte.", "Sollte gleichmäßig sein, ohne Streifen oder Farbverschiebung." } },
            { "Nível de preto", new[] { "Black level", "Nivel de negro", "Niveau de noir", "Schwarzwert" } },
            { "Deve dar para ver os quadrados 2 a 20. Se os primeiros somem, aumente o brilho; se o fundo fica cinza, diminua.", new[] { "Squares 2 to 20 should be visible. If the first ones vanish, raise brightness; if the background turns gray, lower it.", "Deben verse los cuadros 2 a 20. Si los primeros desaparecen, sube el brillo; si el fondo se ve gris, bájalo.", "Les carrés 2 à 20 doivent être visibles. S'ils disparaissent, augmentez la luminosité ; si le fond devient gris, baissez-la.", "Die Quadrate 2 bis 20 sollten sichtbar sein. Verschwinden die ersten, Helligkeit erhöhen; wird der Hintergrund grau, senken." } },
            { "Nível de branco", new[] { "White level", "Nivel de blanco", "Niveau de blanc", "Weißwert" } },
            { "Deve dar para ver os quadrados 235 a 254 sobre o branco. Se somem, diminua o contraste.", new[] { "Squares 235 to 254 should be visible on white. If they vanish, lower contrast.", "Deben verse los cuadros 235 a 254 sobre el blanco. Si desaparecen, baja el contraste.", "Les carrés 235 à 254 doivent être visibles sur le blanc. Sinon, baissez le contraste.", "Die Quadrate 235 bis 254 sollten auf Weiß sichtbar sein. Sonst Kontrast senken." } },
            { "Gama 2.2", new[] { "Gamma 2.2", "Gamma 2.2", "Gamma 2,2", "Gamma 2,2" } },
            { "Olhe de longe ou desfocando a vista: o quadrado central deve se misturar às listras. Mais claro = gama baixa; mais escuro = gama alta.", new[] { "Look from afar or squint: the center square should blend with the stripes. Lighter = low gamma; darker = high gamma.", "Mira de lejos o desenfoca la vista: el cuadro central debe mezclarse con las rayas. Más claro = gamma baja; más oscuro = gamma alta.", "Regardez de loin ou plissez les yeux : le carré central doit se fondre dans les rayures. Plus clair = gamma bas ; plus sombre = gamma élevé.", "Aus der Ferne oder unscharf betrachten: das mittlere Quadrat sollte mit den Streifen verschmelzen. Heller = niedriges Gamma; dunkler = hohes Gamma." } },
            { "Barras de cor", new[] { "Color bars", "Barras de color", "Barres de couleur", "Farbbalken" } },
            { "Compare a saturação. Se um monitor estiver mais vibrante, reduza a saturação dele (modo sRGB ou driver AMD).", new[] { "Compare saturation. If one monitor is more vivid, lower its saturation (sRGB mode or AMD driver).", "Compara la saturación. Si un monitor es más vivo, baja su saturación (modo sRGB o driver AMD).", "Comparez la saturation. Si un écran est plus vif, réduisez sa saturation (mode sRGB ou pilote AMD).", "Vergleiche die Sättigung. Ist ein Monitor kräftiger, Sättigung senken (sRGB-Modus oder AMD-Treiber)." } },
            { "Rampas de cor", new[] { "Color ramps", "Rampas de color", "Dégradés de couleur", "Farbrampen" } },
            { "Vermelho, verde e azul devem crescer suavemente, sem saltos nem mudança de tom.", new[] { "Red, green and blue should rise smoothly, without jumps or hue shifts.", "Rojo, verde y azul deben crecer suavemente, sin saltos ni cambios de tono.", "Rouge, vert et bleu doivent progresser en douceur, sans sauts ni dérive de teinte.", "Rot, Grün und Blau sollten gleichmäßig ansteigen, ohne Sprünge oder Farbverschiebung." } },

            // ---- calibração CRT ----
            { "Grade", new[] { "Grid", "Cuadrícula", "Grille", "Gitter" } },
            { "As linhas devem ser retas e os quadrados iguais em toda a tela. Ajuste geometria: tamanho, posição, almofada (pincushion), trapézio e rotação.", new[] { "Lines should be straight and squares equal across the screen. Adjust geometry: size, position, pincushion, trapezoid and rotation.", "Las líneas deben ser rectas y los cuadros iguales en toda la pantalla. Ajusta geometría: tamaño, posición, cojín, trapecio y rotación.", "Les lignes doivent être droites et les carrés identiques partout. Réglez la géométrie : taille, position, coussin, trapèze et rotation.", "Linien sollten gerade und Quadrate überall gleich sein. Geometrie einstellen: Größe, Position, Kissen, Trapez und Drehung." } },
            { "Monoscópio", new[] { "Monoscope", "Monoscopio", "Mire", "Testbild" } },
            { "Os círculos devem ser redondos (não ovais). Retângulos vermelhos: 90% e 80% (áreas seguras). O centro deve coincidir com o centro físico do tubo.", new[] { "Circles should be round (not oval). Red rectangles: 90% and 80% safe areas. The center should match the tube's physical center.", "Los círculos deben ser redondos (no ovalados). Rectángulos rojos: áreas seguras de 90% y 80%. El centro debe coincidir con el del tubo.", "Les cercles doivent être ronds (pas ovales). Rectangles rouges : zones sûres 90 % et 80 %. Le centre doit coïncider avec celui du tube.", "Kreise sollten rund (nicht oval) sein. Rote Rechtecke: 90 %/80 % sichere Bereiche. Die Mitte sollte mit der Röhrenmitte übereinstimmen." } },
            { "Convergência", new[] { "Convergence", "Convergencia", "Convergence", "Konvergenz" } },
            { "Linhas e pontos devem ser brancos puros. Franjas vermelhas, verdes ou azuis indicam má convergência (ajuste no menu ou ímãs do tubo).", new[] { "Lines and dots should be pure white. Red, green or blue fringes indicate misconvergence (fix in the menu or tube magnets).", "Líneas y puntos deben ser blancos puros. Bordes rojos, verdes o azules indican mala convergencia.", "Lignes et points doivent être blanc pur. Des franges rouges, vertes ou bleues indiquent une mauvaise convergence.", "Linien und Punkte sollten rein weiß sein. Rote, grüne oder blaue Säume bedeuten Fehlkonvergenz." } },
            { "Linearidade", new[] { "Linearity", "Linealidad", "Linéarité", "Linearität" } },
            { "Todos os círculos devem ter o mesmo tamanho e forma. Se os das bordas estão maiores ou achatados, ajuste a linearidade.", new[] { "All circles should have the same size and shape. If edge circles are larger or squashed, adjust linearity.", "Todos los círculos deben tener el mismo tamaño y forma. Si los de los bordes son distintos, ajusta la linealidad.", "Tous les cercles doivent avoir la même taille et forme. Sinon, réglez la linéarité.", "Alle Kreise sollten gleich groß und geformt sein. Sonst Linearität einstellen." } },
            { "Overscan", new[] { "Overscan", "Overscan", "Overscan", "Overscan" } },
            { "Cada borda marca 1% a 10% da imagem. Veja quanto o tubo esconde e ajuste o tamanho até a borda desejada aparecer.", new[] { "Each border marks 1% to 10% of the image. See how much the tube hides and adjust size until the desired border shows.", "Cada borde marca del 1% al 10% de la imagen. Mira cuánto oculta el tubo y ajusta el tamaño.", "Chaque bordure marque 1 % à 10 % de l'image. Voyez ce que le tube masque et ajustez la taille.", "Jeder Rahmen markiert 1 % bis 10 % des Bildes. Prüfe, wie viel die Röhre verdeckt, und passe die Größe an." } },
            { "Barras 16 degraus", new[] { "16-step bars", "Barras de 16 pasos", "Barres 16 paliers", "16-Stufen-Balken" } },
            { "Cada degrau (0 a F) deve ser distinto. Se os primeiros somem, aumente o brilho; se os últimos se fundem, diminua o contraste.", new[] { "Each step (0 to F) should be distinct. If the first vanish, raise brightness; if the last merge, lower contrast.", "Cada paso (0 a F) debe distinguirse. Si los primeros desaparecen, sube el brillo; si los últimos se funden, baja el contraste.", "Chaque palier (0 à F) doit être distinct. Sinon, ajustez luminosité et contraste.", "Jede Stufe (0 bis F) sollte unterscheidbar sein. Sonst Helligkeit/Kontrast anpassen." } },
            { "Nitidez", new[] { "Sharpness", "Nitidez", "Netteté", "Schärfe" } },
            { "Linhas de 1 e 2 pixels e xadrez fino. Devem aparecer separadas e uniformes do centro aos cantos (foco).", new[] { "1 and 2 pixel lines and a fine checkerboard. They should look separate and even from center to corners (focus).", "Líneas de 1 y 2 píxeles y tablero fino. Deben verse separadas y uniformes del centro a las esquinas (foco).", "Lignes de 1 et 2 pixels et damier fin. Elles doivent rester distinctes du centre aux coins (focus).", "1- und 2-Pixel-Linien und feines Schachbrett. Sie sollten von der Mitte bis in die Ecken getrennt sein (Fokus)." } },
            { "Pureza de cor", new[] { "Color purity", "Pureza de color", "Pureté des couleurs", "Farbreinheit" } },
            { "A cor deve ser uniforme em toda a tela, sem manchas (se houver, desmagnetize o tubo). ↑ ↓ trocam a cor.", new[] { "The color should be uniform across the screen, with no blotches (if any, degauss the tube). ↑ ↓ change color.", "El color debe ser uniforme, sin manchas (si las hay, desmagnetiza el tubo). ↑ ↓ cambian el color.", "La couleur doit être uniforme, sans taches (sinon, démagnétisez le tube). ↑ ↓ changent la couleur.", "Die Farbe sollte gleichmäßig sein, ohne Flecken (sonst Röhre entmagnetisieren). ↑ ↓ wechseln die Farbe." } },
            { "Rolagem", new[] { "Scroll", "Desplazamiento", "Défilement", "Scrollen" } },
            { "Listras e quadrado em movimento: verifique rastros, tremulação e geometria em movimento. ↑ ↓ mudam a velocidade.", new[] { "Moving stripes and square: check trails, flicker and geometry in motion. ↑ ↓ change speed.", "Rayas y cuadro en movimiento: revisa estelas, parpadeo y geometría. ↑ ↓ cambian la velocidad.", "Rayures et carré en mouvement : vérifiez traînées, scintillement et géométrie. ↑ ↓ changent la vitesse.", "Bewegte Streifen und Quadrat: Schlieren, Flimmern und Geometrie prüfen. ↑ ↓ ändern die Geschwindigkeit." } },
            { "Grade rolante", new[] { "Scrolling grid", "Cuadrícula en movimiento", "Grille défilante", "Laufendes Gitter" } },
            { "A grade deve se mover sem ondular nem deformar. Ondulação indica fonte ou deflexão instável. ↑ ↓ mudam a velocidade.", new[] { "The grid should move without wobbling or warping. Wobble indicates unstable power or deflection. ↑ ↓ change speed.", "La cuadrícula debe moverse sin ondular ni deformarse. ↑ ↓ cambian la velocidad.", "La grille doit défiler sans onduler ni se déformer. ↑ ↓ changent la vitesse.", "Das Gitter sollte sich ohne Wellen oder Verzerrung bewegen. ↑ ↓ ändern die Geschwindigkeit." } },
        };
    }
}
